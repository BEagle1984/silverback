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

    [Fact]
    public async Task Streaming_ShouldPublishNewStreamToSubscriber_WhenEnumerationIsAbortedWhileOtherSubscriberIsEnumerating()
    {
        int invocationsCount = 0;
        int otherInvocationsCount = 0;
        List<string?> receivedMessages = [];
        List<string?> otherReceivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).AllowStreaming())))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleUnboundedStream)
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventTwo>>(HandleOtherStream));

        async Task HandleUnboundedStream(IAsyncEnumerable<TestEventOne> stream)
        {
            Interlocked.Increment(ref invocationsCount);

            await foreach (TestEventOne message in stream)
            {
                receivedMessages.Add(message.ContentEventOne);

                if (message.ContentEventOne == "2")
                    break;
            }
        }

        async Task HandleOtherStream(IAsyncEnumerable<TestEventTwo> stream)
        {
            Interlocked.Increment(ref otherInvocationsCount);

            await foreach (TestEventTwo message in stream)
            {
                otherReceivedMessages.Add(message.ContentEventTwo);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 4; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
            await producer.ProduceAsync(new TestEventTwo { ContentEventTwo = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedMessages.ShouldBe(["1", "2", "3", "4"]);
        invocationsCount.ShouldBe(2);
        otherReceivedMessages.ShouldBe(["1", "2", "3", "4"]);
        otherInvocationsCount.ShouldBe(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(8);

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.Client.DisconnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task Streaming_ShouldPublishNewStreamToSubscriber_WhenEnumerationIsAbortedWhileOtherSubscriberIsProcessingSameMessage()
    {
        int invocationsCount = 0;
        int otherInvocationsCount = 0;
        List<string?> receivedMessages = [];
        List<string?> otherReceivedMessages = [];
        TaskCompletionSource enumerationAbortedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseOtherSubscriberTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

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
            .AddDelegateSubscriber<IAsyncEnumerable<IIntegrationEvent>>(HandleOtherStream));

        async Task HandleUnboundedStream(IAsyncEnumerable<TestEventOne> stream)
        {
            int invocation = Interlocked.Increment(ref invocationsCount);

            await foreach (TestEventOne message in stream)
            {
                receivedMessages.Add(message.ContentEventOne);

                if (message.ContentEventOne == "2")
                    break;
            }

            if (invocation == 1)
                enumerationAbortedTaskCompletionSource.TrySetResult();
        }

        // Still processing the message when the other subscriber stops enumerating
        async Task HandleOtherStream(IAsyncEnumerable<IIntegrationEvent> stream)
        {
            Interlocked.Increment(ref otherInvocationsCount);

            await foreach (IIntegrationEvent message in stream)
            {
                if (message is TestEventOne { ContentEventOne: "2" })
                    await releaseOtherSubscriberTaskCompletionSource.Task;

                otherReceivedMessages.Add(((TestEventOne)message).ContentEventOne);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 4; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        // The message is committed only once the other subscriber processed it as well
        await enumerationAbortedTaskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(10));
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(1);
        releaseOtherSubscriberTaskCompletionSource.TrySetResult();

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedMessages.ShouldBe(["1", "2", "3", "4"]);
        invocationsCount.ShouldBe(2);
        otherReceivedMessages.ShouldBe(["1", "2", "3", "4"]);
        otherInvocationsCount.ShouldBe(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(4);

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.Client.DisconnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task Streaming_ShouldPublishNewStreamToSubscriber_WhenReturningAfterFirstMessageWhileOtherSubscriberIsEnumerating()
    {
        int invocationsCount = 0;
        int otherInvocationsCount = 0;
        List<string?> receivedMessages = [];
        List<string?> otherReceivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).AllowStreaming())))
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleUnboundedStream)
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventTwo>>(HandleOtherStream));

        void HandleUnboundedStream(IEnumerable<TestEventOne> stream)
        {
            Interlocked.Increment(ref invocationsCount);
            receivedMessages.Add(stream.First().ContentEventOne);
        }

        async Task HandleOtherStream(IAsyncEnumerable<TestEventTwo> stream)
        {
            Interlocked.Increment(ref otherInvocationsCount);

            await foreach (TestEventTwo message in stream)
            {
                otherReceivedMessages.Add(message.ContentEventTwo);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventTwo { ContentEventTwo = "1" });

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await producer.ProduceAsync(new TestEventTwo { ContentEventTwo = "2" });

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedMessages.ShouldBe(["1", "2", "3"]);
        invocationsCount.ShouldBe(3);
        otherReceivedMessages.ShouldBe(["1", "2"]);
        otherInvocationsCount.ShouldBe(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(5);

        // The subscriber is waiting for its next stream, which is never created
        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.Client.DisconnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task Streaming_ShouldPublishMessageToNewStream_WhenEnumerationIsCanceledBeforeReceivingItWhileOtherSubscriberIsEnumerating()
    {
        int invocationsCount = 0;
        List<string?> receivedMessages = [];
        List<string?> otherReceivedMessages = [];
        TaskCompletionSource enumerationCanceledTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource otherReceivedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
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
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleUnboundedStream)
            .AddDelegateSubscriber<IAsyncEnumerable<IIntegrationEvent>>(HandleOtherStream));

        // Cancel the enumeration while waiting for the 2nd message and return only once it's pushed (the other subscriber received it)
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
                    await otherReceivedTaskCompletionSource.Task;
                }
            }
        }

        async Task HandleOtherStream(IAsyncEnumerable<IIntegrationEvent> stream)
        {
            await foreach (IIntegrationEvent message in stream)
            {
                string? content = ((TestEventOne)message).ContentEventOne;
                otherReceivedMessages.Add(content);

                if (content == "2")
                    otherReceivedTaskCompletionSource.TrySetResult();
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await enumerationCanceledTaskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(10));
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedMessages.ShouldBe(["1", "2"]);
        invocationsCount.ShouldBe(2);
        otherReceivedMessages.ShouldBe(["1", "2"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(2);

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.Client.DisconnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task Streaming_ShouldCommitAndPublishNewStream_WhenEnumerationIsAbortedByAllSubscribers()
    {
        int invocationsCount = 0;
        int otherInvocationsCount = 0;
        List<string?> receivedMessages = [];
        List<string?> otherReceivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).AllowStreaming())))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleUnboundedStream)
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventTwo>>(HandleOtherStream));

        async Task HandleUnboundedStream(IAsyncEnumerable<TestEventOne> stream)
        {
            Interlocked.Increment(ref invocationsCount);

            await foreach (TestEventOne message in stream)
            {
                receivedMessages.Add(message.ContentEventOne);

                if (message.ContentEventOne == "2")
                    break;
            }
        }

        // Stops enumerating as well, while the first subscriber waits for its next stream: both get a new stream with the next message
        async Task HandleOtherStream(IAsyncEnumerable<TestEventTwo> stream)
        {
            Interlocked.Increment(ref otherInvocationsCount);

            await foreach (TestEventTwo message in stream)
            {
                otherReceivedMessages.Add(message.ContentEventTwo);

                if (message.ContentEventTwo == "2")
                    break;
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 4; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
            await producer.ProduceAsync(new TestEventTwo { ContentEventTwo = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        receivedMessages.ShouldBe(["1", "2", "3", "4"]);
        invocationsCount.ShouldBe(2);
        otherReceivedMessages.ShouldBe(["1", "2", "3", "4"]);
        otherInvocationsCount.ShouldBe(2);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(8);

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.Client.DisconnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task Streaming_ShouldNotCommitMessage_WhenSubscriberReturnsBecauseConsumerIsStoppingWhileOtherSubscriberIsEnumerating()
    {
        int invocationsCount = 0;
        TaskCompletionSource processingStartedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).AllowStreaming())))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>, CancellationToken>(HandleUnboundedStream)
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventTwo>>(HandleOtherStream));

        async Task HandleUnboundedStream(IAsyncEnumerable<TestEventOne> stream, CancellationToken cancellationToken)
        {
            Interlocked.Increment(ref invocationsCount);

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

        static async Task HandleOtherStream(IAsyncEnumerable<TestEventTwo> stream)
        {
            await foreach (TestEventTwo dummy in stream)
            {
                // Just enumerate, ignoring the cancellation token
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventTwo());
        await producer.ProduceAsync(new TestEventOne());
        await processingStartedTaskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(10));

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.StopAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        invocationsCount.ShouldBe(1);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(1);
    }

    [Fact]
    public async Task Streaming_ShouldNotCommitMessageNotReceived_WhenSubscriberReturnsBecauseConsumerIsStoppingWhileOtherSubscriberIsEnumerating()
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
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).AllowStreaming())))
            .AddSingletonBrokerBehavior(holdMessageBehavior)
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>, CancellationToken>(HandleUnboundedStream)
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventTwo>>(HandleOtherStream));

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

        static async Task HandleOtherStream(IAsyncEnumerable<TestEventTwo> stream)
        {
            await foreach (TestEventTwo dummy in stream)
            {
                // Just enumerate, ignoring the cancellation token
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventTwo());
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
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(2);
    }

    [Fact]
    public async Task Streaming_ShouldNotCommitMessage_WhenSubscriberThrowsBecauseConsumerIsStoppingWhileOtherSubscriberIsEnumerating()
    {
        TaskCompletionSource processingStartedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).AllowStreaming())))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>, CancellationToken>(HandleUnboundedStream)
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventTwo>>(HandleOtherStream));

        async Task HandleUnboundedStream(IAsyncEnumerable<TestEventOne> stream, CancellationToken cancellationToken)
        {
            await foreach (TestEventOne dummy in stream)
            {
                processingStartedTaskCompletionSource.TrySetResult();
                await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
            }
        }

        static async Task HandleOtherStream(IAsyncEnumerable<TestEventTwo> stream)
        {
            await foreach (TestEventTwo dummy in stream)
            {
                // Just enumerate, ignoring the cancellation token
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventTwo());
        await producer.ProduceAsync(new TestEventOne());
        await processingStartedTaskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(10));

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.StopAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(1);
    }

    [Fact]
    public async Task Streaming_ShouldCommitAndPublishNewStream_WhenStreamIsNotEnumeratedWhileOtherSubscriberIsEnumerating()
    {
        int invocationsCount = 0;
        List<string?> otherReceivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).AllowStreaming())))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleUnboundedStream)
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventTwo>>(HandleOtherStream));

        // Returns without receiving the message it's invoked for, which is committed anyway
        Task HandleUnboundedStream(IAsyncEnumerable<TestEventOne> stream)
        {
            Interlocked.Increment(ref invocationsCount);
            return Task.CompletedTask;
        }

        async Task HandleOtherStream(IAsyncEnumerable<TestEventTwo> stream)
        {
            await foreach (TestEventTwo message in stream)
            {
                otherReceivedMessages.Add(message.ContentEventTwo);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventTwo { ContentEventTwo = "1" });

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        await producer.ProduceAsync(new TestEventTwo { ContentEventTwo = "2" });

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        invocationsCount.ShouldBe(3);
        otherReceivedMessages.ShouldBe(["1", "2"]);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(5);

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.Client.DisconnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task Streaming_ShouldDisconnect_WhenSubscriberInvokedAgainWithNewStreamFails()
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
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).AllowStreaming())))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleUnboundedStream)
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventTwo>>(HandleOtherStream));

        // Stops enumerating after the 1st message, then fails processing the 2nd one in the new stream
        async Task HandleUnboundedStream(IAsyncEnumerable<TestEventOne> stream)
        {
            Interlocked.Increment(ref invocationsCount);

            await foreach (TestEventOne message in stream)
            {
                receivedMessages.Add(message.ContentEventOne);

                if (message.ContentEventOne == "1")
                    break;

                throw new InvalidOperationException("Test");
            }
        }

        static async Task HandleOtherStream(IAsyncEnumerable<TestEventTwo> stream)
        {
            await foreach (TestEventTwo dummy in stream)
            {
                // Just enumerate
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventTwo());

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        receivedMessages.ShouldBe(["1", "2"]);
        invocationsCount.ShouldBe(2);
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(2);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
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
