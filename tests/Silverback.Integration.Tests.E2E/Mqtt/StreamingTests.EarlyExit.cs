// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class StreamingTests
{
    [Fact]
    public async Task Streaming_ShouldAcknowledgeAndPublishNewStream_WhenEnumerationIsAbortedWithoutException()
    {
        int invocationsCount = 0;
        List<string?> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .AllowStreaming())))
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

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.ShouldBe(["1", "2", "3", "4"]);
        invocationsCount.ShouldBe(2);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.Client.DisconnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task Streaming_ShouldAcknowledgeAndPublishNewStream_WhenReturningAfterFirstMessage()
    {
        int invocationsCount = 0;
        List<string?> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .AllowStreaming())))
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

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.ShouldBe(["1", "2", "3"]);
        invocationsCount.ShouldBe(3);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.Client.DisconnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task Streaming_ShouldAcknowledgeAndPublishNewStream_WhenEnumerationIsAbortedAndOtherStreamSubscriberNeverReceivesMessages()
    {
        int invocationsCount = 0;
        List<string?> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .AllowStreaming())))
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

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.ShouldBe(["1", "2", "3", "4"]);
        invocationsCount.ShouldBe(2);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.Client.DisconnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task Streaming_ShouldAcknowledgeAndPublishToOtherSubscribers_WhenStreamIsNotEnumerated()
    {
        int invocationsCount = 0;
        List<string?> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .AllowStreaming())))
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

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.ShouldBe(["1", "2", "3"]);
        invocationsCount.ShouldBe(3);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);

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
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .AllowStreaming())))
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

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.ShouldBe(["1", "2"]);
        invocationsCount.ShouldBe(2);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.Client.DisconnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
    }

    [Fact]
    public async Task Streaming_ShouldNotAcknowledgeMessage_WhenSubscriberReturnsBecauseConsumerIsStopping()
    {
        int tryCount = 0;
        List<string?> receivedMessages = [];
        TaskCompletionSource processingStartedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .AllowStreaming())))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>, CancellationToken>(HandleUnboundedStream));

        [SuppressMessage("ReSharper", "UseCancellationTokenForIAsyncEnumerable", Justification = "The stop aborts the stream, the token cancels the processing")]
        async Task HandleUnboundedStream(IAsyncEnumerable<TestEventOne> stream, CancellationToken cancellationToken)
        {
            try
            {
                await foreach (TestEventOne message in stream)
                {
                    receivedMessages.Add(message.ContentEventOne);

                    // Only the first attempt is canceled by the stop
                    if (Interlocked.Increment(ref tryCount) > 1)
                        continue;

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
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await processingStartedTaskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(10));

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.StopAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));

        DefaultClientSession.GetPendingMessagesCount().ShouldBe(1);

        // MQTT can't seek: the message is replayed from memory when the consumer is restarted
        await consumer.StartAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.ShouldBe(["1", "1"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Streaming_ShouldNotAcknowledgeMessageNotReceived_WhenSubscriberReturnsBecauseConsumerIsStopping()
    {
        List<string?> receivedMessages = [];
        HoldMessageBehavior holdMessageBehavior = new("2");
        TaskCompletionSource enumerationCanceledTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .AllowStreaming())))
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
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // Stop the consumer while the 2nd message is being processed, before it's pushed to the stream
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });
        await holdMessageBehavior.ReachedTask.WaitAsync(TimeSpan.FromSeconds(10));

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        Task stopTask = consumer.StopAsync().AsTask();
        await enumerationCanceledTaskCompletionSource.Task.WaitAsync(TimeSpan.FromSeconds(10));
        holdMessageBehavior.Release();
        await stopTask.WaitAsync(TimeSpan.FromSeconds(10));

        receivedMessages.ShouldBe(["1"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(1);

        // MQTT can't seek: the message is replayed from memory when the consumer is restarted
        await consumer.StartAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.ShouldBe(["1", "2"]);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
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
