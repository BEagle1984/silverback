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
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class StreamingTests
{
    [Fact]
    public async Task Streaming_ShouldDisconnect_WhenAsyncEnumerableProcessingFails()
    {
        List<TestEventOne> receivedMessages = [];

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
        List<TestEventOne> receivedMessages = [];

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
        List<TestEventOne> receivedMessages = [];
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
