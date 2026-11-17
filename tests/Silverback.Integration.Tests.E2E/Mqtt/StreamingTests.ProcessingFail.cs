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
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class StreamingTests
{
    [Fact]
    public async Task Streaming_ShouldDisconnect_WhenAsyncEnumerableProcessingFails()
    {
        TestingCollection<TestEventOne> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .RequestPersistentSession()
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .AllowStreaming())))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleUnboundedStream));

        async ValueTask HandleUnboundedStream(IAsyncEnumerable<TestEventOne> enumerable)
        {
            await foreach (TestEventOne message in enumerable)
            {
                receivedMessages.Add(message);

                if (receivedMessages.Count == 2)
                    throw new InvalidOperationException("Test");
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        MqttConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<MqttConsumer>().Single();
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        receivedMessages.Count.ShouldBe(2);

        // Only the first message is acknowledged
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(2);
    }

    [Fact]
    public async Task Streaming_ShouldDisconnect_WhenObservableProcessingFails()
    {
        TestingCollection<TestEventOne> receivedMessages = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .AsObservable()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .RequestPersistentSession()
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .AllowStreaming())))
            .AddDelegateSubscriber<IMessageStreamObservable<TestEventOne>>(HandleUnboundedStream));

        void HandleUnboundedStream(IMessageStreamObservable<TestEventOne> observable) =>
            observable.Subscribe(message =>
            {
                receivedMessages.Add(message);

                if (receivedMessages.Count == 2)
                    throw new InvalidOperationException("Test");
            });

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        MqttConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<MqttConsumer>().Single();
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        receivedMessages.Count.ShouldBe(2);

        // Only the first message is acknowledged
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(2);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    public async Task Streaming_ShouldApplyErrorPolicyOnceToFailedMessage_WhenProcessingFails(int failingMessageNumber)
    {
        TestingCollection<TestEventOne> receivedMessages = [];
        StopCallback callback = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .RequestPersistentSession()
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .AllowStreaming())))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleUnboundedStream)
            .AddSingletonErrorPolicyCallback(callback));

        async ValueTask HandleUnboundedStream(IAsyncEnumerable<TestEventOne> enumerable)
        {
            await foreach (TestEventOne message in enumerable)
            {
                receivedMessages.Add(message);

                if (receivedMessages.Count == failingMessageNumber)
                    throw new InvalidOperationException("Test");
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        MqttConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<MqttConsumer>().Single();
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        receivedMessages.Count.ShouldBe(failingMessageNumber);

        // The messages preceding the failed one are acknowledged
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(3 - (failingMessageNumber - 1));

        IReadOnlyList<(string? Content, Exception Exception)> stoppingCalls = callback.StoppingCalls;
        stoppingCalls.Count.ShouldBe(1);
        stoppingCalls[0].Content.ShouldBe($"{failingMessageNumber}");
        stoppingCalls[0].Exception.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Test");
    }

    [Fact]
    public async Task Streaming_ShouldRollBackFailedMessageAndDisconnectWithoutAcknowledging_WhenErrorHandlingFails()
    {
        TestingCollection<TestEventOne> receivedMessages = [];
        TransactionEventsTracker transactionEventsTracker = new();

        // The error handling fails, since the callback invoked when the consumer is stopped throws (the error policies can't be used
        // when streaming)
        IStopConsumerErrorPolicyCallback callback = Substitute.For<IStopConsumerErrorPolicyCallback>();
        callback.OnStoppingConsumerAsync(Arg.Any<IRawInboundEnvelope>(), Arg.Any<Exception>())
            .Returns(Task.FromException(new InvalidOperationException("Callback failed.")));

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .RequestPersistentSession()
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .AllowStreaming())))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleUnboundedStream)
            .AddSingletonBrokerBehavior(transactionEventsTracker)
            .AddSingletonErrorPolicyCallback(callback));

        async ValueTask HandleUnboundedStream(IAsyncEnumerable<TestEventOne> enumerable)
        {
            await foreach (TestEventOne message in enumerable)
            {
                receivedMessages.Add(message);

                if (receivedMessages.Count == 2)
                    throw new InvalidOperationException("Test");
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        // The first message is acknowledged by its own pipeline, while the failed one is rolled back, raising the Aborting and Aborted
        // events, as when the error policies don't handle the error
        MqttConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<MqttConsumer>().Single();
        await AsyncTestingUtil.WaitAsync(() =>
            consumer.Client.Status == ClientStatus.Disconnected && transactionEventsTracker.Events.Contains("Aborted"));

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        receivedMessages.Count.ShouldBe(2);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(2);
        transactionEventsTracker.Events.Where(transactionEvent => transactionEvent.StartsWith("Abort", StringComparison.Ordinal))
            .ShouldBe(["Aborting", "Aborted"]);
        transactionEventsTracker.Events.Count(transactionEvent => transactionEvent == "Committed").ShouldBe(1);
    }

    [Fact]
    public async Task Streaming_ShouldApplyErrorPolicyOnceAndDisconnect_WhenConcurrentProcessingFailsWhileNoMessageIsPushed()
    {
        int subscriberCallsCount = 0;
        TaskCompletionSource secondMessageReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StopCallback callback = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .RequestPersistentSession()
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .AllowStreaming())))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleUnboundedStream)
            .AddSingletonErrorPolicyCallback(callback));

        async Task HandleUnboundedStream(IAsyncEnumerable<TestEventOne> enumerable)
        {
            Interlocked.Increment(ref subscriberCallsCount);

            await Parallel.ForEachAsync(enumerable, new ParallelOptions { MaxDegreeOfParallelism = 2 }, ProcessMessageAsync);
        }

        async ValueTask ProcessMessageAsync(TestEventOne message, CancellationToken cancellationToken)
        {
            if (message.ContentEventOne != "1")
            {
                secondMessageReceived.TrySetResult();
                return;
            }

            // Fail once the second message is acknowledged, while the stream waits for the next message
            await AsyncTestingUtil.WaitAsync(() =>
                secondMessageReceived.Task.IsCompleted && DefaultClientSession.GetPendingMessagesCount() == 0);
            throw new InvalidOperationException("Test");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });

        MqttConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<MqttConsumer>().Single();
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        subscriberCallsCount.ShouldBe(1);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);

        // The error policies are applied to the message that started the stream, since the failed message isn't known
        IReadOnlyList<(string? Content, Exception Exception)> stoppingCalls = callback.StoppingCalls;
        stoppingCalls.Count.ShouldBe(1);
        stoppingCalls[0].Content.ShouldBe("1");
        stoppingCalls[0].Exception.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Test");
    }

    [Fact]
    public async Task Streaming_ShouldDisconnectWithoutRollingBackAcknowledgedMessage_WhenErrorHandlingFailsWhileNoMessageIsPushed()
    {
        TaskCompletionSource secondMessageReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TransactionEventsTracker transactionEventsTracker = new();

        // The error handling fails, since the callback invoked when the consumer is stopped throws
        StopCallback callback = new(() => Task.FromException(new InvalidOperationException("Callback failed.")));

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .RequestPersistentSession()
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .AllowStreaming())))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleUnboundedStream)
            .AddSingletonBrokerBehavior(transactionEventsTracker)
            .AddSingletonErrorPolicyCallback(callback));

        Task HandleUnboundedStream(IAsyncEnumerable<TestEventOne> enumerable) =>
            Parallel.ForEachAsync(enumerable, new ParallelOptions { MaxDegreeOfParallelism = 2 }, ProcessMessageAsync);

        async ValueTask ProcessMessageAsync(TestEventOne message, CancellationToken cancellationToken)
        {
            if (message.ContentEventOne != "1")
            {
                secondMessageReceived.TrySetResult();
                return;
            }

            // Fail once the second message is acknowledged, while the stream waits for the next message
            await AsyncTestingUtil.WaitAsync(() =>
                secondMessageReceived.Task.IsCompleted && DefaultClientSession.GetPendingMessagesCount() == 0);
            throw new InvalidOperationException("Test");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });

        MqttConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<MqttConsumer>().Single();
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        // The error handling is applied to the message that started the stream, which was acknowledged by its own pipeline: the consumer
        // is just stopped, without rolling back its transaction again
        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
        callback.StoppingCalls.Count.ShouldBe(1);
        transactionEventsTracker.Events.ShouldNotContain("Aborting");
        transactionEventsTracker.Events.ShouldNotContain("Aborted");
        transactionEventsTracker.Events.Count(transactionEvent => transactionEvent == "Committed").ShouldBe(2);
    }

    [Fact]
    public async Task Streaming_ShouldNotStartNewStream_WhenMessageIsConsumedWhileHandlingErrorThrownWhileNoMessageIsPushed()
    {
        int subscriberCallsCount = 0;
        TaskCompletionSource secondMessageReceived = new(TaskCreationOptions.RunContinuationsAsynchronously);

        // Keep handling the error until the next message disconnects the consumer
        StopCallback callback = new(() => AsyncTestingUtil.WaitAsync(() =>
            Helper.GetConsumerForEndpoint(DefaultTopicName).Client.Status == ClientStatus.Disconnected));

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .RequestPersistentSession()
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .AllowStreaming())))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleUnboundedStream)
            .AddSingletonErrorPolicyCallback(callback)
            .AddIntegrationSpy());

        async Task HandleUnboundedStream(IAsyncEnumerable<TestEventOne> enumerable)
        {
            Interlocked.Increment(ref subscriberCallsCount);

            await Parallel.ForEachAsync(enumerable, new ParallelOptions { MaxDegreeOfParallelism = 2 }, ProcessMessageAsync);
        }

        async ValueTask ProcessMessageAsync(TestEventOne message, CancellationToken cancellationToken)
        {
            if (message.ContentEventOne != "1")
            {
                secondMessageReceived.TrySetResult();
                return;
            }

            // Fail once the second message is acknowledged, while the stream waits for the next message
            await AsyncTestingUtil.WaitAsync(() =>
                secondMessageReceived.Task.IsCompleted && DefaultClientSession.GetPendingMessagesCount() == 0);
            throw new InvalidOperationException("Test");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });

        await AsyncTestingUtil.WaitAsync(() => callback.StoppingCalls.Count > 0);
        callback.StoppingCalls.Count.ShouldBe(1);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });

        MqttConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<MqttConsumer>().Single();
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);

        // The third message was consumed (but not acknowledged), without starting a new stream
        Helper.Spy.RawInboundEnvelopes.Count.ShouldBe(3);
        consumer.StatusInfo.LatestConsumedMessageIdentifier.ShouldBe(Helper.Spy.RawInboundEnvelopes[2].BrokerMessageIdentifier);
        subscriberCallsCount.ShouldBe(1);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(1);
        callback.StoppingCalls.Count.ShouldBe(1);
    }

    [Fact]
    public async Task Streaming_ShouldApplyErrorPolicyOnce_WhenStopAfterErrorThrownWhileNoMessageIsPushedCancelsOtherSubscriber()
    {
        TaskCompletionSource<bool> otherSubscriberWaiting = new(TaskCreationOptions.RunContinuationsAsynchronously);
        StopCallback callback = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .RequestPersistentSession()
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .AllowStreaming())))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleUnboundedStream)
            .AddDelegateSubscriber<TestEventOne, CancellationToken>(HandleMessageAsync)
            .AddSingletonErrorPolicyCallback(callback));

        async Task HandleUnboundedStream(IAsyncEnumerable<TestEventOne> enumerable) =>
            await Parallel.ForEachAsync(enumerable, new ParallelOptions { MaxDegreeOfParallelism = 2 }, ProcessMessageAsync);

        async ValueTask ProcessMessageAsync(TestEventOne message, CancellationToken cancellationToken)
        {
            if (message.ContentEventOne != "1")
                return;

            // Fail once the second message was pushed and is being handled by the other subscriber
            await otherSubscriberWaiting.Task;
            throw new InvalidOperationException("Test");
        }

        async Task HandleMessageAsync(TestEventOne message, CancellationToken cancellationToken)
        {
            if (message.ContentEventOne != "2")
                return;

            // Canceled by the stop
            otherSubscriberWaiting.TrySetResult(true);
            await Task.Delay(Timeout.Infinite, cancellationToken);
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });

        MqttConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<MqttConsumer>().Single();
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);

        // The second message is rolled back
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(1);

        IReadOnlyList<(string? Content, Exception Exception)> stoppingCalls = callback.StoppingCalls;
        stoppingCalls.Count.ShouldBe(1);
        stoppingCalls[0].Content.ShouldBe("1");
        stoppingCalls[0].Exception.ShouldBeOfType<InvalidOperationException>().Message.ShouldBe("Test");
    }

    private sealed class StopCallback : IStopConsumerErrorPolicyCallback
    {
        private readonly Func<Task>? _onStopping;

        private readonly List<(string? Content, Exception Exception)> _stoppingCalls = [];

        public StopCallback(Func<Task>? onStopping = null)
        {
            _onStopping = onStopping;
        }

        public IReadOnlyList<(string? Content, Exception Exception)> StoppingCalls
        {
            get
            {
                lock (_stoppingCalls)
                {
                    return [.. _stoppingCalls];
                }
            }
        }

        public Task OnStoppingConsumerAsync(IRawInboundEnvelope envelope, Exception exception)
        {
            lock (_stoppingCalls)
            {
                _stoppingCalls.Add((((envelope as IInboundEnvelope)?.Message as TestEventOne)?.ContentEventOne, exception));
            }

            return _onStopping?.Invoke() ?? Task.CompletedTask;
        }
    }
}
