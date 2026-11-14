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
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class StreamingTests
{
    [Fact]
    public async Task Streaming_ShouldAbortEnumeration_WhenDisconnecting()
    {
        bool aborted = false;
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
            .AddDelegateSubscriber<IMessageStreamEnumerable<TestEventOne>>(HandleUnboundedStream));

        async ValueTask HandleUnboundedStream(IEnumerable<TestEventOne> stream)
        {
            try
            {
                foreach (TestEventOne message in stream)
                {
                    receivedMessages.Add(message);
                }
            }
            catch (OperationCanceledException)
            {
                // Simulate something going on in the subscribed method
                await Task.Delay(300);
                aborted = true;
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Count.ShouldBe(2);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);

        await Helper.GetConsumerForEndpoint(DefaultTopicName).Client.DisconnectAsync();
        await AsyncTestingUtil.WaitAsync(() => aborted);

        aborted.ShouldBeTrue();
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Streaming_ShouldCompleteObservable_WhenDisconnecting()
    {
        bool completed = false;
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
            observable.Subscribe(receivedMessages.Add, () => completed = true);

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Count.ShouldBe(2);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);

        await Helper.GetConsumerForEndpoint(DefaultTopicName).Client.DisconnectAsync();
        await AsyncTestingUtil.WaitAsync(() => completed);

        completed.ShouldBeTrue();
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Streaming_ShouldPublishNewStream_WhenReconnecting()
    {
        int invocationsCount = 0;
        int canceledCount = 0;
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

        // Return as soon as the enumeration is aborted, before the abort completes
        async Task HandleUnboundedStream(IAsyncEnumerable<TestEventOne> stream)
        {
            Interlocked.Increment(ref invocationsCount);

            try
            {
                await foreach (TestEventOne message in stream)
                {
                    receivedMessages.Add(message);
                }
            }
            catch (OperationCanceledException)
            {
                Interlocked.Increment(ref canceledCount);
            }
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });

        // Reconnect once both messages are acknowledged, while the stream waits for the next message
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        await consumer.Client.DisconnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        await consumer.Client.ConnectAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(10));
        await Helper.WaitUntilConnectedAsync();

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedMessages.Select(message => message.ContentEventOne).ShouldBe(["1", "2", "3"]);
        invocationsCount.ShouldBe(2);
        canceledCount.ShouldBe(1);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Streaming_ShouldNotApplyErrorPolicy_WhenConsumerStopsWhileOtherSubscriberHandlesFirstMessage()
    {
        TaskCompletionSource<bool> otherSubscriberStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> streamCanceled = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseOtherSubscriber = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IStopConsumerErrorPolicyCallback callback = Substitute.For<IStopConsumerErrorPolicyCallback>();

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
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>, CancellationToken>(HandleUnboundedStream)
            .AddDelegateSubscriber<TestEventOne>(HandleMessageAsync)
            .AddSingletonErrorPolicyCallback(callback));

        // Throws the cancellation while waiting for the next message, after the push of the first one completed
        async Task HandleUnboundedStream(IAsyncEnumerable<TestEventOne> stream, CancellationToken cancellationToken)
        {
            try
            {
                await foreach (TestEventOne dummy in stream.WithCancellation(cancellationToken))
                {
                    // Request the next message
                }
            }
            catch (OperationCanceledException)
            {
                streamCanceled.TrySetResult(true);
                throw;
            }
        }

        // Ignores the cancellation (e.g. an I/O call), completing the processing of the first message after the stream was aborted
        async Task HandleMessageAsync(TestEventOne message)
        {
            otherSubscriberStarted.TrySetResult(true);
            await releaseOtherSubscriber.Task;
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await otherSubscriberStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

        IConsumer consumer = Helper.GetConsumerForEndpoint(DefaultTopicName);
        Task stopTask = consumer.StopAsync().AsTask();
        await streamCanceled.Task.WaitAsync(TimeSpan.FromSeconds(10));

        // Let the abort caused by the stream cancellation complete before the first message's processing completes
        await Task.Delay(200);
        releaseOtherSubscriber.SetResult(true);
        await stopTask.WaitAsync(TimeSpan.FromSeconds(10));

        callback.ReceivedCalls().ShouldBeEmpty();
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        consumer.Client.Status.ShouldBe(ClientStatus.Initialized);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }
}
