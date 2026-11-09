// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class StreamingTests
{
    [Fact]
    public async Task Streaming_ShouldCommitAndPublishNewStream_WhenEnumerationIsAbortedWithoutException()
    {
        int invocationsCount = 0;
        List<string?> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName).AllowStreaming())))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleUnboundedStream));

        async Task HandleUnboundedStream(IAsyncEnumerable<TestEventOne> stream)
        {
            Interlocked.Increment(ref invocationsCount);

            int count = 0;
            await foreach (TestEventOne message in stream)
            {
                receivedMessages.Add(message.ContentEventOne);

                if (++count >= 2)
                    break;
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 4; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedMessages.ShouldBe(["1", "2", "3", "4"]);
        invocationsCount.ShouldBe(2);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(4);

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.Client.DisconnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task Streaming_ShouldCommitAndPublishNewStream_WhenReturningAfterFirstMessage()
    {
        int invocationsCount = 0;
        List<string?> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName).AllowStreaming())))
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleUnboundedStream));

        void HandleUnboundedStream(IEnumerable<TestEventOne> stream)
        {
            Interlocked.Increment(ref invocationsCount);
            receivedMessages.Add(stream.First().ContentEventOne);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedMessages.ShouldBe(["1", "2", "3"]);
        invocationsCount.ShouldBe(3);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(3);

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.Client.DisconnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task Streaming_ShouldCommitAndPublishNewStream_WhenEnumerationIsAbortedAndOtherStreamSubscriberNeverReceivesMessages()
    {
        int invocationsCount = 0;
        List<string?> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName).AllowStreaming())))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleUnboundedStream)
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventTwo>>(HandleOtherStream));

        async Task HandleUnboundedStream(IAsyncEnumerable<TestEventOne> stream)
        {
            Interlocked.Increment(ref invocationsCount);

            int count = 0;
            await foreach (TestEventOne message in stream)
            {
                receivedMessages.Add(message.ContentEventOne);

                if (++count >= 2)
                    break;
            }
        }

        // E.g., the subscriber of a batch consumed from another endpoint, which gets a stream that is never created
        static async Task HandleOtherStream(IAsyncEnumerable<TestEventTwo> stream)
        {
            await foreach (TestEventTwo dummy in stream)
            {
                // Never invoked
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 4; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedMessages.ShouldBe(["1", "2", "3", "4"]);
        invocationsCount.ShouldBe(2);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(4);

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.Client.DisconnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task Streaming_ShouldCommitAndPublishToOtherSubscribers_WhenStreamIsNotEnumerated()
    {
        int invocationsCount = 0;
        List<string?> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName).AllowStreaming())))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleUnboundedStream)
            .AddDelegateSubscriber<TestEventOne>(HandleMessage));

        Task HandleUnboundedStream(IAsyncEnumerable<TestEventOne> stream)
        {
            Interlocked.Increment(ref invocationsCount);
            return Task.CompletedTask;
        }

        void HandleMessage(TestEventOne message) => receivedMessages.Add(message.ContentEventOne);

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedMessages.ShouldBe(["1", "2", "3"]);
        invocationsCount.ShouldBe(3);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(3);

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.Client.DisconnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task Streaming_ShouldPublishMessageToNewStream_WhenEnumerationIsCanceledBeforeReceivingIt()
    {
        int invocationsCount = 0;
        List<string?> receivedMessages = [];
        TaskCompletionSource enumerationCanceledTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        HoldMessageBehavior holdMessageBehavior = new("2");

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName).AllowStreaming())))
            .AddSingletonBrokerBehavior(holdMessageBehavior)
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleUnboundedStream));

        // Cancel the enumeration while waiting for the 2nd message, which is pushed before the subscriber returns
        async Task HandleUnboundedStream(IAsyncEnumerable<TestEventOne> stream)
        {
            int invocation = Interlocked.Increment(ref invocationsCount);
            using CancellationTokenSource cancellationTokenSource = new();

            try
            {
                await foreach (TestEventOne message in stream.WithCancellation(cancellationTokenSource.Token))
                {
                    receivedMessages.Add(message.ContentEventOne);

                    if (invocation == 1)
                        cancellationTokenSource.CancelAfter(200);
                }
            }
            catch (OperationCanceledException)
            {
                if (invocation == 1)
                {
                    enumerationCanceledTaskCompletionSource.TrySetResult();

                    await holdMessageBehavior.ReachedTask;
                    holdMessageBehavior.Release();
                    await Task.Delay(200);
                }
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await enumerationCanceledTaskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedMessages.ShouldBe(["1", "2"]);
        invocationsCount.ShouldBe(2);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(2);

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.Client.DisconnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task Streaming_ShouldNotCommitMessage_WhenSubscriberReturnsBecauseConsumerIsStopping()
    {
        TaskCompletionSource processingStartedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName).AllowStreaming())))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>, CancellationToken>(HandleUnboundedStream));

        async Task HandleUnboundedStream(IAsyncEnumerable<TestEventOne> stream, CancellationToken cancellationToken)
        {
            try
            {
                await foreach (TestEventOne dummy in stream)
                {
                    processingStartedTaskCompletionSource.TrySetResult();
                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
            }
            catch (OperationCanceledException)
            {
                // Stop processing without throwing
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await processingStartedTaskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(10));

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.StopAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(0);
    }

    [Fact]
    public async Task Streaming_ShouldNotCommitMessageNotReceived_WhenSubscriberReturnsBecauseConsumerIsStopping()
    {
        List<string?> receivedMessages = [];
        HoldMessageBehavior holdMessageBehavior = new("2");
        TaskCompletionSource enumerationCanceledTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom(DefaultTopicName).AllowStreaming())))
            .AddSingletonBrokerBehavior(holdMessageBehavior)
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>, CancellationToken>(HandleUnboundedStream));

        async Task HandleUnboundedStream(IAsyncEnumerable<TestEventOne> stream, CancellationToken cancellationToken)
        {
            try
            {
                await foreach (TestEventOne message in stream.WithCancellation(cancellationToken))
                {
                    receivedMessages.Add(message.ContentEventOne);
                }
            }
            catch (OperationCanceledException)
            {
                // Stop enumerating without throwing
                enumerationCanceledTaskCompletionSource.TrySetResult();
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        // Stop the consumer while the 2nd message is being processed, before it's pushed to the stream
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });
        await holdMessageBehavior.ReachedTask.WaitAsync(TimeSpan.FromSeconds(10));

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        Task stopTask = consumer.StopAsync().AsTask();
        await enumerationCanceledTaskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(10));
        holdMessageBehavior.Release();
        await stopTask.WaitAsync(TimeSpan.FromSeconds(10));

        receivedMessages.ShouldBe(["1"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(1);
    }

    private sealed class HoldMessageBehavior : IConsumerBehavior
    {
        private readonly string _content;

        private readonly TaskCompletionSource _reachedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        private readonly TaskCompletionSource _releaseTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public HoldMessageBehavior(string content)
        {
            _content = content;
        }

        public Task ReachedTask => _reachedTaskCompletionSource.Task;

        public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.Publisher - 1;

        public void Release() => _releaseTaskCompletionSource.TrySetResult();

        public async ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
        {
            if (context.Envelope is IInboundEnvelope { Message: TestEventOne message } && message.ContentEventOne == _content)
            {
                _reachedTaskCompletionSource.TrySetResult();
                await _releaseTaskCompletionSource.Task;
            }

            await next(context, cancellationToken);
        }
    }
}
