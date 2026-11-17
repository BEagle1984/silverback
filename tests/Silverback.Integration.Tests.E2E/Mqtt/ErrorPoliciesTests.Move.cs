// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Buffers;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class ErrorPoliciesTests
{
    [Fact]
    public async Task MovePolicy_ShouldMoveMessageToOtherTopic()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo("other-topic"))
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy.MoveTo("other-topic")))))
            .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
            .AddIntegrationSpy());

        static void HandleMessage(IIntegrationEvent unused) => throw new InvalidOperationException("Move!");

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);

        Helper.Spy.OutboundEnvelopes[1].Message.ShouldBeEquivalentTo(Helper.Spy.OutboundEnvelopes[0].Message);
        Helper.Spy.OutboundEnvelopes[1].GetEndpoint().RawName.ShouldBe("other-topic");

        IReadOnlyList<MqttApplicationMessage> messages = Helper.GetMessages("other-topic");
        messages.Count.ShouldBe(1);
        messages[0].Payload.ToArray().ShouldBe(Helper.Spy.InboundEnvelopes[0].RawMessage.ReReadAll());
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task MovePolicy_ShouldStopConsumerWithoutAcknowledging_WhenMoveFails()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .RequestPersistentSession()
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo("other-topic"))
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .OnError(policy => policy.MoveTo(
                            "other-topic",
                            movePolicy => movePolicy.Transform(
                                _ => throw new InvalidOperationException("Move failed!")))))))
            .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
            .AddIntegrationSpy());

        static void HandleMessage(IIntegrationEvent unused) => throw new InvalidOperationException("Move!");

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(1);
        Helper.GetMessages("other-topic").Count.ShouldBe(0);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(1);
    }

    [Fact]
    public async Task MovePolicy_ShouldConsumeMessageAgain_WhenMoveFailedAndPersistentSessionIsResumed()
    {
        int moveCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .RequestPersistentSession()
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo("other-topic"))
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .OnError(policy => policy.MoveTo("other-topic", movePolicy => movePolicy.Transform(FailFirstMove))))))
            .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
            .AddIntegrationSpy());

        void FailFirstMove(IOutboundEnvelope? unused)
        {
            if (Interlocked.Increment(ref moveCount) == 1)
                throw new InvalidOperationException("Move failed!");
        }

        static void HandleMessage(IIntegrationEvent unused) => throw new InvalidOperationException("Move!");

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(1);

        // Like a broker, the persistent session redelivers the unacknowledged QoS 1 message when it's resumed
        await consumer.Client.ConnectAsync();

        await Helper.WaitUntilConnectedAsync();
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        moveCount.ShouldBe(2);
        Helper.Spy.InboundEnvelopes.Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne).ShouldBe(["1", "1"]);
        Helper.GetMessages("other-topic").Count.ShouldBe(1);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task MovePolicy_ShouldNotConsumeMessageAgain_WhenMoveFailedAndCleanSessionIsStarted()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .RequestCleanSession()
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo("other-topic"))
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtLeastOnceQoS()
                        .OnError(policy => policy.MoveTo(
                            "other-topic",
                            movePolicy => movePolicy.Transform(
                                _ => throw new InvalidOperationException("Move failed!")))))))
            .AddDelegateSubscriber<TestEventOne>(HandleMessage)
            .AddIntegrationSpy());

        static void HandleMessage(TestEventOne message)
        {
            if (message.ContentEventOne == "1")
                throw new InvalidOperationException("Move!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);

        // Like a broker, the clean session discards the unacknowledged message on disconnect
        await consumer.Client.ConnectAsync();

        await Helper.WaitUntilConnectedAsync();

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne).ShouldBe(["1", "2"]);
        Helper.GetMessages("other-topic").Count.ShouldBe(0);
        Helper.GetClientSession(DefaultClientId).GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task MovePolicy_ShouldNotConsumeMessageAgain_WhenMoveFailedWithAtMostOnceQoSAndPersistentSessionIsResumed()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .RequestPersistentSession()
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo("other-topic"))
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .WithAtMostOnceQoS()
                        .OnError(policy => policy.MoveTo(
                            "other-topic",
                            movePolicy => movePolicy.Transform(
                                _ => throw new InvalidOperationException("Move failed!")))))))
            .AddDelegateSubscriber<TestEventOne>(HandleMessage)
            .AddIntegrationSpy());

        static void HandleMessage(TestEventOne message)
        {
            if (message.ContentEventOne == "1")
                throw new InvalidOperationException("Move!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        await AsyncTestingUtil.WaitAsync(() => consumer.Client.Status == ClientStatus.Disconnected);

        consumer.Client.Status.ShouldBe(ClientStatus.Disconnected);

        // Like a broker, the persistent session never redelivers a QoS 0 message: it's lost and no longer pending
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);

        await consumer.Client.ConnectAsync();

        await Helper.WaitUntilConnectedAsync();

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Select(envelope => ((TestEventOne)envelope.Message!).ContentEventOne).ShouldBe(["1", "2"]);
        Helper.GetMessages("other-topic").Count.ShouldBe(0);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task MovePolicy_ShouldMoveMessageToSameTopicAndRetry()
    {
        TestEventOne message = new() { ContentEventOne = "Hello E2E!" };
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy.MoveTo(
                            DefaultTopicName,
                            movePolicy => movePolicy.WithMaxRetries(10))))))
            .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
            .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent unused)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.RawOutboundEnvelopes.Count.ShouldBe(11);
        tryCount.ShouldBe(11);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(11);
        Helper.Spy.InboundEnvelopes.ForEach(envelope => envelope.Message.ShouldBeEquivalentTo(message));
    }

    [Fact]
    public async Task MovePolicy_ShouldMoveMessageToOtherTopicAfterRetry()
    {
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo("other-topic"))
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy.Retry(1).ThenMoveTo("other-topic")))))
            .AddDelegateSubscriber<IIntegrationEvent>(HandleMessage)
            .AddIntegrationSpy());

        void HandleMessage(IIntegrationEvent unused)
        {
            tryCount++;
            throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(2);
        Helper.Spy.InboundEnvelopes.Count.ShouldBe(2);

        tryCount.ShouldBe(2);

        Helper.Spy.OutboundEnvelopes[1].Message.ShouldBeEquivalentTo(Helper.Spy.OutboundEnvelopes[0].Message);
        Helper.Spy.OutboundEnvelopes[1].GetEndpoint().RawName.ShouldBe("other-topic");

        IReadOnlyList<MqttApplicationMessage> messages = Helper.GetMessages("other-topic");
        messages.Count.ShouldBe(1);
        messages[0].Payload.ToArray().ShouldBe(Helper.Spy.InboundEnvelopes[0].RawMessage.ReReadAll());
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task MovePolicy_ShouldMoveAndAcknowledgeMessage_WhenOtherEndpointBatchIsPending()
    {
        TestingCollection<List<string?>> receivedBatches = [];

        // The endpoints of a client share the batch: only a message failing before being added to it (e.g. not deserializable) is
        // moved while the batch is pending
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("other-topic"))
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom("e2e/batch")
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(3, TimeSpan.FromMinutes(5)))
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom("e2e/move")
                        .WithAtLeastOnceQoS()
                        .EnableBatchProcessing(3, TimeSpan.FromMinutes(5))
                        .OnError(policy => policy.MoveTo("other-topic")))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];
            receivedBatches.Add(list);

            await foreach (TestEventOne message in batch)
            {
                list.Add(message.ContentEventOne);
            }
        }

        IProducer batchProducer = Helper.GetProducerForEndpoint("e2e/batch");
        IProducer moveProducer = Helper.GetProducerForEndpoint("e2e/move");

        await batchProducer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await AsyncTestingUtil.WaitAsync(() => receivedBatches.Count == 1);

        await moveProducer.RawProduceAsync("not json"u8.ToArray());

        // The moved message is acknowledged right away, while the batch is pending
        await AsyncTestingUtil.WaitAsync(() =>
            Helper.GetMessages("other-topic").Count == 1 && DefaultClientSession.GetPendingMessagesCount("e2e/move") == 0);
        Helper.GetMessages("other-topic").ShouldHaveSingleItem().Payload.ToArray().ShouldBe("not json"u8.ToArray());
        DefaultClientSession.GetPendingMessagesCount("e2e/move").ShouldBe(0);
        DefaultClientSession.GetPendingMessagesCount("e2e/batch").ShouldBe(1);

        await moveProducer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });
        await batchProducer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        receivedBatches.ShouldHaveSingleItem().ShouldBe(["1", "2", "3"]);
        Helper.GetMessages("other-topic").Count.ShouldBe(1);
        DefaultClientSession.GetPendingMessagesCount().ShouldBe(0);
    }
}
