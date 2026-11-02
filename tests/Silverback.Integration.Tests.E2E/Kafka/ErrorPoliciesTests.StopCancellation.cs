// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ErrorPoliciesTests
{
    [Theory]
    [InlineData("skip")]
    [InlineData("retry")]
    public async Task ErrorPolicy_ShouldLeaveConsumerStopped_WhenRollbackFailsWhileStopping(string policy)
    {
        TaskCompletionSource<bool> processingStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> rollbackFailed = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .DisableAutoCommit()
                    .CommitOffsetEach(1)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(errorPolicy => ConfigureErrorPolicy(errorPolicy, policy)))))
            .AddDelegateSubscriber<IIntegrationEvent, ISilverbackContext, CancellationToken>(HandleMessageAsync));

        async Task HandleMessageAsync(IIntegrationEvent message, ISilverbackContext context, CancellationToken cancellationToken)
        {
            // The failed rollback makes the error policy reconnect the consumer
            context.GetConsumerPipelineContext().TransactionManager.Aborting.AddHandler(_ =>
            {
                rollbackFailed.TrySetResult(true);
                throw new InvalidOperationException("Rollback failed.");
            });

            processingStarted.TrySetResult(true);

            try
            {
                // Canceled by the stop
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Not a cancellation, for the error policy to be applied
                throw new InvalidOperationException("Processing failed.");
            }
        }

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });
        await processingStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // The blocking stop waits for the processing requesting the reconnect. A deadlock makes this wait time out but also hangs the
        // host disposal, which stops the same consumer, so the run then only ends through the blame hang timeout.
        await consumer.StopAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        rollbackFailed.Task.IsCompleted.ShouldBeTrue();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
        consumer.Client.Status.ShouldBe(ClientStatus.Initialized);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
    }

    [Theory]
    [InlineData("skip")]
    [InlineData("stop")]
    [InlineData("retry")]
    public async Task ErrorPolicy_ShouldNotBeApplied_WhenStopCancelsProcessing(string policy)
    {
        TaskCompletionSource<bool> processingStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        ISkipMessageErrorPolicyCallback callback =
            Substitute.For<ISkipMessageErrorPolicyCallback, IRetryErrorPolicyCallback, IStopConsumerErrorPolicyCallback>();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .DisableAutoCommit()
                    .CommitOffsetEach(1)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(errorPolicy => ConfigureErrorPolicy(errorPolicy, policy)))))
            .AddDelegateSubscriber<IIntegrationEvent, CancellationToken>(HandleMessageAsync)
            .AddSingletonErrorPolicyCallback(callback));

        async Task HandleMessageAsync(IIntegrationEvent message, CancellationToken cancellationToken)
        {
            processingStarted.TrySetResult(true);

            // Canceled by the stop
            await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
        }

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });
        await processingStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // The blocking stop waits for the canceled processing
        await consumer.StopAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        callback.ReceivedCalls().ShouldBeEmpty();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
        consumer.Client.Status.ShouldBe(ClientStatus.Initialized);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);

        await consumer.StartAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        consumer.StatusInfo.Status.ShouldNotBe(ConsumerStatus.Stopped);
    }

    [Fact]
    public async Task StopPolicy_ShouldNotBeApplied_WhenStopCancelsStreamProcessing()
    {
        TaskCompletionSource<bool> processingStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IStopConsumerErrorPolicyCallback callback = Substitute.For<IStopConsumerErrorPolicyCallback>();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .DisableAutoCommit()
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .AllowStreaming()
                        .OnError(errorPolicy => errorPolicy.Stop()))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>, CancellationToken>(HandleStreamAsync)
            .AddSingletonErrorPolicyCallback(callback));

        async Task HandleStreamAsync(IAsyncEnumerable<TestEventOne> stream, CancellationToken cancellationToken)
        {
            await foreach (TestEventOne dummy in stream)
            {
                processingStarted.TrySetResult(true);

                // Canceled by the stop
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });
        await processingStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // The blocking stop waits for the canceled processing
        await consumer.StopAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        callback.ReceivedCalls().ShouldBeEmpty();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
        consumer.Client.Status.ShouldBe(ClientStatus.Initialized);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);

        await consumer.StartAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        consumer.StatusInfo.Status.ShouldNotBe(ConsumerStatus.Stopped);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task StopPolicy_ShouldStopAndDisconnect_WhenStreamSubscriberThrowsOperationCanceledExceptionNotCausedByStop(bool allowStreaming)
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .DisableAutoCommit()
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint =>
                    {
                        endpoint.ConsumeFrom(DefaultTopicName).OnError(policy => policy.Stop());

                        if (allowStreaming)
                            endpoint.AllowStreaming();
                        else
                            endpoint.EnableBatchProcessing(5);
                    })))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleStreamAsync));

        static async Task HandleStreamAsync(IAsyncEnumerable<TestEventOne> stream)
        {
            await foreach (TestEventOne dummy in stream)
            {
                // Not caused by the stop (e.g. a timed out HTTP request): the error policies applied aborting the sequence stop the
                // consumer, canceling the token, before the exception reaches the transaction handler
                throw new TaskCanceledException("Unrelated timeout");
            }
        }

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Hello E2E!" });

        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected, TimeSpan.FromSeconds(10));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
    }

    private static void ConfigureErrorPolicy(IErrorPolicyBuilder errorPolicy, string policy)
    {
        switch (policy)
        {
            case "skip":
                errorPolicy.Skip();
                break;
            case "stop":
                errorPolicy.Stop();
                break;
            default:
                errorPolicy.Retry(10);
                break;
        }
    }
}
