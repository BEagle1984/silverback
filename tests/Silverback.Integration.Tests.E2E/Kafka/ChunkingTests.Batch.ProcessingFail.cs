// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ChunkingTests
{
    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task Chunking_ShouldApplyErrorPoliciesOnce_WhenChunkedMessageAddedToBatchFails(bool retryBeforeStopping)
    {
        int batchesCount = 0;
        int stopCalls = 0;
        int retryCalls = 0;

        IStopConsumerErrorPolicyCallback stopCallback = Substitute.For<IStopConsumerErrorPolicyCallback>();
        stopCallback.OnStoppingConsumerAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref stopCalls);
                return Task.CompletedTask;
            });
        IRetryErrorPolicyCallback retryCallback = Substitute.For<IRetryErrorPolicyCallback>();
        retryCallback.OnRetryingMessageAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref retryCalls);
                return Task.CompletedTask;
            });

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(3)
                        .OnError(policy =>
                        {
                            if (retryBeforeStopping)
                                policy.Retry(1).ThenStop();
                            else
                                policy.Stop();
                        }))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddSingletonErrorPolicyCallback(stopCallback)
            .AddSingletonErrorPolicyCallback(retryCallback)
            .AddIntegrationSpy());

        // The subscriber fails processing the chunked message added to the batch started by the previous message
        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            Interlocked.Increment(ref batchesCount);

            await foreach (TestEventOne message in batch)
            {
                if (message.ContentEventOne == "Long message")
                    throw new InvalidOperationException("Test");
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        byte[][] chunks = SerializeAndSplitIntoChunks("Long message", 3);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Short message" });

        for (int i = 0; i < chunks.Length; i++)
        {
            await producer.RawProduceAsync(chunks[i], HeadersHelper.GetChunkHeaders("long", i, chunks.Length, typeof(TestEventOne)));
        }

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected, TimeSpan.FromSeconds(10));

        // Give a second application of the error policies the time to happen
        await Task.Delay(500);

        batchesCount.ShouldBe(retryBeforeStopping ? 2 : 1);
        retryCalls.ShouldBe(retryBeforeStopping ? 1 : 0);
        stopCalls.ShouldBe(1);
        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
    }

    [Theory]
    [InlineData(false, false)]
    [InlineData(false, true)]
    [InlineData(true, false)]
    [InlineData(true, true)]
    public async Task Chunking_ShouldStopConsumerAndDisconnect_WhenBatchOfChunkedMessagesFails(bool retryBeforeStopping, bool throwImmediately)
    {
        int batchesCount = 0;
        int stopCalls = 0;

        IStopConsumerErrorPolicyCallback stopCallback = Substitute.For<IStopConsumerErrorPolicyCallback>();
        stopCallback.OnStoppingConsumerAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(_ =>
            {
                Interlocked.Increment(ref stopCalls);
                return Task.CompletedTask;
            });

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName).EnableChunking(10)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(2)
                        .OnError(policy =>
                        {
                            if (retryBeforeStopping)
                                policy.Retry(2);
                            else
                                policy.Stop();
                        }))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddSingletonErrorPolicyCallback(stopCallback)
            .AddIntegrationSpy());

        // The batch is started by a chunked message, and ended by one (unless the subscriber fails before it's added), whose reader awaits
        // the processing of the chunk sequence instead of the batch
        async ValueTask HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            Interlocked.Increment(ref batchesCount);

            if (!throwImmediately)
            {
                await foreach (TestEventOne dummy in batch)
                {
                    // Enumerate the whole batch before failing
                }
            }

            throw new InvalidOperationException("Test");
        }

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "Long message 1" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "Long message 2" });

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected, TimeSpan.FromSeconds(10));

        // Give a second application of the error policies the time to happen
        await Task.Delay(500);

        batchesCount.ShouldBe(retryBeforeStopping ? 3 : 1);
        stopCalls.ShouldBe(1);
        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
    }
}
