// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Protocol;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker.Mqtt.Mocks;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Testing.Messaging.Broker.Mqtt.Mocks;

public class InMemoryMqttBrokerTests
{
    [Fact]
    public void GetMessages_ShouldReturnEmpty_WhenTopicHasNoMessages()
    {
        using InMemoryMqttBroker broker = new();

        broker.GetMessages("topic").ShouldBeEmpty();
    }

    [Fact]
    public async Task GetMessages_ShouldReturnSnapshot()
    {
        using InMemoryMqttBroker broker = new();
        MockedMqttClient client = await CreateConnectedClientAsync(broker);
        await client.PublishAsync(CreateMessage("topic"));

        IReadOnlyList<MqttApplicationMessage> messages = broker.GetMessages("topic");
        await client.PublishAsync(CreateMessage("topic"));

        messages.Count.ShouldBe(1);
        broker.GetMessages("topic").Count.ShouldBe(2);
    }

    [Fact]
    public async Task GetMessages_ShouldNotThrow_WhenMessagesArePublishedConcurrently()
    {
        using InMemoryMqttBroker broker = new();
        MockedMqttClient client = await CreateConnectedClientAsync(broker);
        using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(5));
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        Task producer = Task.Run(
            async () =>
            {
                for (int i = 0; i < 10_000 && !cancellationToken.IsCancellationRequested; i++)
                {
                    // Publish to a new topic every few messages to also grow the dictionary
                    await client.PublishAsync(CreateMessage($"topic{i / 10}"));
                }
            });

        while (!producer.IsCompleted)
        {
            Should.NotThrow(
                () =>
                {
                    foreach (MqttApplicationMessage message in broker.GetMessages("topic1"))
                    {
                        message.ShouldNotBeNull();
                    }
                });
        }

        await producer;
        broker.GetMessages("topic1").Count.ShouldBe(10);
    }

    [Fact]
    public async Task PublishAsync_ShouldDistributeSharedSubscriptionMessagesRoundRobin()
    {
        using InMemoryMqttBroker broker = new();
        MockedMqttClient publisher = await CreateConnectedClientAsync(broker);
        ConcurrentQueue<string> received1 = new();
        ConcurrentQueue<string> received2 = new();
        await CreateSubscribedClientAsync(broker, "consumer1", received1, "$share/group/topic");
        await CreateSubscribedClientAsync(broker, "consumer2", received2, "$share/group/topic");

        for (int i = 1; i <= 4; i++)
        {
            await publisher.PublishAsync(CreateMessage("topic", $"{i}"));
        }

        await WaitUntilAllMessagesAreConsumedAsync(broker);

        received1.Count.ShouldBe(2);
        received2.Count.ShouldBe(2);
        received1.Union(received2).ShouldBe(["1", "2", "3", "4"], true);
    }

    [Fact]
    public async Task PublishAsync_ShouldDeliverSharedSubscriptionMessagesToConnectedSessions()
    {
        using InMemoryMqttBroker broker = new();
        MockedMqttClient publisher = await CreateConnectedClientAsync(broker);
        ConcurrentQueue<string> received1 = new();
        ConcurrentQueue<string> received2 = new();
        MockedMqttClient consumer1 = await CreateSubscribedClientAsync(broker, "consumer1", received1, ["$share/group/topic"], false);
        await CreateSubscribedClientAsync(broker, "consumer2", received2, "$share/group/topic");
        await consumer1.DisconnectAsync();

        for (int i = 1; i <= 4; i++)
        {
            await publisher.PublishAsync(CreateMessage("topic", $"{i}"));
        }

        await WaitUntilAllMessagesAreConsumedAsync(broker);

        received1.ShouldBeEmpty();
        received2.ShouldBe(["1", "2", "3", "4"]);
        broker.GetClientSession("consumer1").GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task PublishAsync_ShouldQueueSharedSubscriptionMessagesInPersistentSessions_WhenNoSessionIsConnected()
    {
        using InMemoryMqttBroker broker = new();
        MockedMqttClient publisher = await CreateConnectedClientAsync(broker);
        ConcurrentQueue<string> received1 = new();
        ConcurrentQueue<string> received2 = new();
        MockedMqttClient consumer1 = await CreateSubscribedClientAsync(broker, "consumer1", received1, ["$share/group/topic"], false);
        MockedMqttClient consumer2 = await CreateSubscribedClientAsync(broker, "consumer2", received2, ["$share/group/topic"], false);
        MqttClientOptions options1 = consumer1.Options!;
        MqttClientOptions options2 = consumer2.Options!;
        await consumer1.DisconnectAsync();
        await consumer2.DisconnectAsync();

        await publisher.PublishAsync(CreateMessage("topic", "1"));
        await publisher.PublishAsync(CreateMessage("topic", "2"));

        await consumer1.ConnectAsync(options1);
        await consumer2.ConnectAsync(options2);
        await WaitUntilAllMessagesAreConsumedAsync(broker);

        received1.ShouldBe(["1"]);
        received2.ShouldBe(["2"]);
    }

    [Fact]
    public async Task PublishAsync_ShouldDeliverToEachSharedSubscription_WhenGroupSharesDifferentFilters()
    {
        using InMemoryMqttBroker broker = new();
        MockedMqttClient publisher = await CreateConnectedClientAsync(broker);
        ConcurrentQueue<string> received1 = new();
        ConcurrentQueue<string> received2 = new();
        await CreateSubscribedClientAsync(broker, "consumer1", received1, "$share/group/topic/#");
        await CreateSubscribedClientAsync(broker, "consumer2", received2, "$share/group/topic/+");

        await publisher.PublishAsync(CreateMessage("topic/a", "1"));

        await WaitUntilAllMessagesAreConsumedAsync(broker);

        received1.ShouldBe(["1"]);
        received2.ShouldBe(["1"]);
    }

    [Fact]
    public async Task PublishAsync_ShouldNotDeliverDollarTopic_WhenFilterStartsWithWildcard()
    {
        using InMemoryMqttBroker broker = new();
        MockedMqttClient publisher = await CreateConnectedClientAsync(broker);
        ConcurrentQueue<string> received1 = new();
        ConcurrentQueue<string> received2 = new();
        await CreateSubscribedClientAsync(broker, "consumer1", received1, "#");
        await CreateSubscribedClientAsync(broker, "consumer2", received2, "$SYS/#");

        await publisher.PublishAsync(CreateMessage("$SYS/uptime", "1"));
        await publisher.PublishAsync(CreateMessage("topic", "2"));

        await WaitUntilAllMessagesAreConsumedAsync(broker);

        received1.ShouldBe(["2"]);
        received2.ShouldBe(["1"]);
    }

    [Fact]
    public async Task Subscribe_ShouldKeepSharedAndPlainSubscriptions_WhenSubscribingSharedFilterAgain()
    {
        using InMemoryMqttBroker broker = new();
        MockedMqttClient publisher = await CreateConnectedClientAsync(broker);
        ConcurrentQueue<string> received1 = new();
        ConcurrentQueue<string> received2 = new();
        MockedMqttClient consumer1 = await CreateSubscribedClientAsync(broker, "consumer1", received1, "$share/group/topic", "topic");
        await CreateSubscribedClientAsync(broker, "consumer2", received2, "$share/group/topic");
        await consumer1.SubscribeAsync(
            new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter("$share/group/topic")
                .Build());

        await publisher.PublishAsync(CreateMessage("topic", "1"));
        await publisher.PublishAsync(CreateMessage("topic", "2"));

        await WaitUntilAllMessagesAreConsumedAsync(broker);

        // The plain subscription delivers all messages to consumer1, while the shared one delivers an additional copy alternating
        // between the two consumers
        received1.ShouldBe(["1", "1", "2"]);
        received2.ShouldBe(["2"]);
    }

    [Fact]
    public async Task Unsubscribe_ShouldRemoveSharedSubscriptionOnly()
    {
        using InMemoryMqttBroker broker = new();
        MockedMqttClient publisher = await CreateConnectedClientAsync(broker);
        ConcurrentQueue<string> received1 = new();
        ConcurrentQueue<string> received2 = new();
        MockedMqttClient consumer1 = await CreateSubscribedClientAsync(broker, "consumer1", received1, "$share/group/topic", "topic");
        await CreateSubscribedClientAsync(broker, "consumer2", received2, "$share/group/topic");

        await consumer1.UnsubscribeAsync(
            new MqttClientUnsubscribeOptionsBuilder()
                .WithTopicFilter("$share/group/topic")
                .Build());
        await publisher.PublishAsync(CreateMessage("topic", "1"));
        await publisher.PublishAsync(CreateMessage("topic", "2"));

        await WaitUntilAllMessagesAreConsumedAsync(broker);

        // consumer1 still receives through the plain subscription, while consumer2 is the only member of the share group
        received1.ShouldBe(["1", "2"]);
        received2.ShouldBe(["1", "2"]);
    }

    [Fact]
    public async Task PublishAsync_ShouldDistributeSharedSubscriptionMessagesPerBroker()
    {
        using InMemoryMqttBroker broker = new();
        MockedMqttClient publisher1 = await CreateClientAsync(broker, "publisher1", true, "server1");
        MockedMqttClient publisher2 = await CreateClientAsync(broker, "publisher2", true, "server2");
        ConcurrentQueue<string> received1 = new();
        ConcurrentQueue<string> received2 = new();
        ConcurrentQueue<string> received3 = new();
        ConcurrentQueue<string> received4 = new();
        await CreateSubscribedClientAsync(broker, "consumer1", received1, ["$share/group/topic"], true, "server1");
        await CreateSubscribedClientAsync(broker, "consumer2", received2, ["$share/group/topic"], true, "server1");
        await CreateSubscribedClientAsync(broker, "consumer3", received3, ["$share/group/topic"], true, "server2");
        await CreateSubscribedClientAsync(broker, "consumer4", received4, ["$share/group/topic"], true, "server2");

        for (int i = 1; i <= 2; i++)
        {
            await publisher1.PublishAsync(CreateMessage("topic", $"server1-{i}"));
            await publisher2.PublishAsync(CreateMessage("topic", $"server2-{i}"));
        }

        await WaitUntilAllMessagesAreConsumedAsync(broker);

        received1.Count.ShouldBe(1);
        received2.Count.ShouldBe(1);
        received3.Count.ShouldBe(1);
        received4.Count.ShouldBe(1);
    }

    [Fact]
    public async Task WaitUntilAllMessagesAreConsumedAsync_ShouldWaitForTopicsMatchingFilter()
    {
        using InMemoryMqttBroker broker = new();
        MockedMqttClient publisher = await CreateConnectedClientAsync(broker);
        TaskCompletionSource<bool> releaseHandler = new(TaskCreationOptions.RunContinuationsAsynchronously);
        MockedMqttClient consumer = await CreateClientAsync(broker, "consumer");
        consumer.ApplicationMessageReceivedAsync += _ => releaseHandler.Task;
        await consumer.SubscribeAsync(
            new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter("topic/+")
                .Build());

        await publisher.PublishAsync(CreateMessage("topic/a", "1"));

        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        Task filterWait = broker.WaitUntilAllMessagesAreConsumedAsync(["topic/+"], timeout.Token);
        Task sharedFilterWait = broker.WaitUntilAllMessagesAreConsumedAsync(["$share/group/topic/#"], timeout.Token);
        await broker.WaitUntilAllMessagesAreConsumedAsync(["other/+"], timeout.Token);

        filterWait.IsCompleted.ShouldBeFalse();
        sharedFilterWait.IsCompleted.ShouldBeFalse();

        releaseHandler.SetResult(true);
        await filterWait;
        await sharedFilterWait;
    }

    // Publishing concurrently to many new topics makes it likely that a message is handled right after being written to the channel
    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The tasks are awaited before disposing")]
    public async Task WaitUntilAllMessagesAreConsumedAsync_ShouldComplete_WhenMessagesOfManyTopicsArePublishedConcurrently()
    {
        using InMemoryMqttBroker broker = new();
        MockedMqttClient publisher = await CreateConnectedClientAsync(broker);
        ConcurrentQueue<string> received = new();
        await CreateSubscribedClientAsync(broker, "consumer", received, "topic/#");

        await Task.WhenAll(
            Enumerable.Range(0, 2 * Environment.ProcessorCount).Select(task => Task.Run(async () =>
            {
                for (int i = 0; i < 100; i++)
                {
                    await publisher.PublishAsync(CreateMessage($"topic/{task}/{i}"));
                }
            })));

        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        await broker.WaitUntilAllMessagesAreConsumedAsync(["topic/#"], timeout.Token);

        received.Count.ShouldBe(2 * Environment.ProcessorCount * 100);
        broker.GetClientSession("consumer").GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task PublishAsync_ShouldDeliverSingleCopy_WhenOverlappingSubscriptionsMatch()
    {
        using InMemoryMqttBroker broker = new();
        MockedMqttClient publisher = await CreateConnectedClientAsync(broker);
        ConcurrentQueue<string> received = new();
        await CreateSubscribedClientAsync(broker, "consumer", received, "topic/#", "topic/a", "topic/+");

        await publisher.PublishAsync(CreateMessage("topic/a", "1"));

        await WaitUntilAllMessagesAreConsumedAsync(broker);

        received.ShouldBe(["1"]);
        broker.GetClientSession("consumer").GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The task is awaited before disposing")]
    public async Task PublishAsync_ShouldNotThrow_WhenClientsConnectAndDisconnectConcurrently()
    {
        using InMemoryMqttBroker broker = new();
        MockedMqttClient publisher = await CreateConnectedClientAsync(broker);
        using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromSeconds(5));
        CancellationToken cancellationToken = cancellationTokenSource.Token;

        Task connecting = Task.Run(
            async () =>
            {
                for (int i = 0; i < 1_000 && !cancellationToken.IsCancellationRequested; i++)
                {
                    MockedMqttClient client = await CreateClientAsync(broker, $"consumer{i}");
                    await client.DisconnectAsync();
                }
            });

        while (!connecting.IsCompleted)
        {
            await Should.NotThrowAsync(() => publisher.PublishAsync(CreateMessage("topic")));
        }

        await connecting;
    }

    [Fact]
    public async Task Connect_ShouldRedeliverUnacknowledgedMessagesBeforeNewerOnes_WhenPersistentSessionIsResumed()
    {
        using InMemoryMqttBroker broker = new();
        MockedMqttClient publisher = await CreateConnectedClientAsync(broker);
        ConcurrentQueue<MqttApplicationMessageReceivedEventArgs> received = new();
        MockedMqttClient consumer = await CreateManuallyAcknowledgingClientAsync(broker, "consumer", false, received);
        MqttClientOptions options = consumer.Options!;

        for (int i = 1; i <= 3; i++)
        {
            await publisher.PublishAsync(CreateMessage("topic", $"{i}", MqttQualityOfServiceLevel.AtLeastOnce));
        }

        await AsyncTestingUtil.WaitAsync(() => received.Count >= 3);
        await received.First().AcknowledgeAsync(CancellationToken.None);
        await consumer.DisconnectAsync();
        await publisher.PublishAsync(CreateMessage("topic", "4", MqttQualityOfServiceLevel.AtLeastOnce));

        broker.GetClientSession("consumer").GetPendingMessagesCount().ShouldBe(3);

        await consumer.ConnectAsync(options);
        await AsyncTestingUtil.WaitAsync(() => received.Count >= 6);

        GetPayloads(received).ShouldBe(["1", "2", "3", "2", "3", "4"]);

        foreach (MqttApplicationMessageReceivedEventArgs eventArgs in received.Skip(3))
        {
            await eventArgs.AcknowledgeAsync(CancellationToken.None);
        }

        broker.GetClientSession("consumer").GetPendingMessagesCount().ShouldBe(0);
        broker.GetClientSession("consumer").GetPendingMessagesCount("topic").ShouldBe(0);
    }

    [Fact]
    public async Task Connect_ShouldRedeliverMessage_WhenAcknowledgedAfterDisconnect()
    {
        using InMemoryMqttBroker broker = new();
        MockedMqttClient publisher = await CreateConnectedClientAsync(broker);
        ConcurrentQueue<MqttApplicationMessageReceivedEventArgs> received = new();
        MockedMqttClient consumer = await CreateManuallyAcknowledgingClientAsync(broker, "consumer", false, received);
        MqttClientOptions options = consumer.Options!;

        await publisher.PublishAsync(CreateMessage("topic", "1", MqttQualityOfServiceLevel.AtLeastOnce));
        await AsyncTestingUtil.WaitAsync(() => !received.IsEmpty);
        await consumer.DisconnectAsync();

        // The broker can't receive the acknowledgment once the connection is closed
        await received.Single().AcknowledgeAsync(CancellationToken.None);

        broker.GetClientSession("consumer").GetPendingMessagesCount().ShouldBe(1);

        await consumer.ConnectAsync(options);
        await AsyncTestingUtil.WaitAsync(() => received.Count >= 2);
        await received.Last().AcknowledgeAsync(CancellationToken.None);

        GetPayloads(received).ShouldBe(["1", "1"]);
        broker.GetClientSession("consumer").GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Connect_ShouldIgnoreAcknowledgmentOfPreviousConnection_WhenMessageWasRedelivered()
    {
        using InMemoryMqttBroker broker = new();
        MockedMqttClient publisher = await CreateConnectedClientAsync(broker);
        ConcurrentQueue<MqttApplicationMessageReceivedEventArgs> received = new();
        MockedMqttClient consumer = await CreateManuallyAcknowledgingClientAsync(broker, "consumer", false, received);
        MqttClientOptions options = consumer.Options!;

        await publisher.PublishAsync(CreateMessage("topic", "1", MqttQualityOfServiceLevel.AtLeastOnce));
        await AsyncTestingUtil.WaitAsync(() => !received.IsEmpty);
        await consumer.DisconnectAsync();
        await consumer.ConnectAsync(options);
        await AsyncTestingUtil.WaitAsync(() => received.Count >= 2);

        // The acknowledgment of the first delivery must not acknowledge the redelivery
        await received.First().AcknowledgeAsync(CancellationToken.None);

        broker.GetClientSession("consumer").GetPendingMessagesCount().ShouldBe(1);

        await consumer.DisconnectAsync();
        await consumer.ConnectAsync(options);
        await AsyncTestingUtil.WaitAsync(() => received.Count >= 3);
        await received.Last().AcknowledgeAsync(CancellationToken.None);

        GetPayloads(received).ShouldBe(["1", "1", "1"]);
        broker.GetClientSession("consumer").GetPendingMessagesCount().ShouldBe(0);
    }

    [Fact]
    public async Task Connect_ShouldRedeliverSharedSubscriptionCopyOnly_WhenOnlySharedSubscriptionHasHigherQualityOfService()
    {
        using InMemoryMqttBroker broker = new();
        MockedMqttClient publisher = await CreateConnectedClientAsync(broker);
        ConcurrentQueue<MqttApplicationMessageReceivedEventArgs> received = new();
        MockedMqttClient consumer = await CreateManuallyAcknowledgingClientAsync(
            broker,
            "consumer",
            false,
            received,
            MqttQualityOfServiceLevel.AtMostOnce);
        await SubscribeAsync(consumer, "$share/group/topic", MqttQualityOfServiceLevel.AtLeastOnce);
        MqttClientOptions options = consumer.Options!;

        await publisher.PublishAsync(CreateMessage("topic", "1", MqttQualityOfServiceLevel.AtLeastOnce));
        await AsyncTestingUtil.WaitAsync(() => received.Count >= 2);
        await consumer.DisconnectAsync();

        await consumer.ConnectAsync(options);
        await publisher.PublishAsync(CreateMessage("topic", "2", MqttQualityOfServiceLevel.AtLeastOnce));
        await AsyncTestingUtil.WaitAsync(() => received.Count >= 5);

        // The plain subscription copy (QoS 0) is lost, while the shared subscription copy (QoS 1) precedes the newer messages
        GetPayloads(received).ShouldBe(["1", "1", "1", "2", "2"]);
    }

    [Fact]
    public async Task Connect_ShouldPreserveRedeliveryOrder_WhenDisconnectedAgainDuringRedelivery()
    {
        using InMemoryMqttBroker broker = new();
        MockedMqttClient publisher = await CreateConnectedClientAsync(broker);
        ConcurrentQueue<MqttApplicationMessageReceivedEventArgs> received = new();
        TaskCompletionSource<bool> releaseRedelivery = new(TaskCreationOptions.RunContinuationsAsynchronously);
        MockedMqttClient consumer = await CreateClientAsync(broker, "consumer", false);
        MqttClientOptions options = consumer.Options!;
        consumer.ApplicationMessageReceivedAsync += async args =>
        {
            args.AutoAcknowledge = false;
            received.Enqueue(args);

            // Block the first redelivery, so that the second message is still to be redelivered when disconnecting
            if (received.Count == 3)
                await releaseRedelivery.Task;
        };
        await SubscribeAsync(consumer, "topic", MqttQualityOfServiceLevel.AtLeastOnce);

        await publisher.PublishAsync(CreateMessage("topic", "1", MqttQualityOfServiceLevel.AtLeastOnce));
        await publisher.PublishAsync(CreateMessage("topic", "2", MqttQualityOfServiceLevel.AtLeastOnce));
        await AsyncTestingUtil.WaitAsync(() => received.Count >= 2);
        await consumer.DisconnectAsync();

        await consumer.ConnectAsync(options);
        await AsyncTestingUtil.WaitAsync(() => received.Count >= 3);
        await consumer.DisconnectAsync();
        releaseRedelivery.SetResult(true);

        await consumer.ConnectAsync(options);
        await AsyncTestingUtil.WaitAsync(() => received.Count >= 5);

        GetPayloads(received).ShouldBe(["1", "2", "1", "1", "2"]);
        broker.GetClientSession("consumer").GetPendingMessagesCount().ShouldBe(2);
    }

    [Theory]
    [InlineData(MqttQualityOfServiceLevel.AtMostOnce, MqttQualityOfServiceLevel.AtLeastOnce)]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, MqttQualityOfServiceLevel.AtMostOnce)]
    public async Task Connect_ShouldNotRedeliverUnacknowledgedMessages_WhenQualityOfServiceIsAtMostOnce(
        MqttQualityOfServiceLevel subscriptionQualityOfServiceLevel,
        MqttQualityOfServiceLevel messageQualityOfServiceLevel)
    {
        using InMemoryMqttBroker broker = new();
        MockedMqttClient publisher = await CreateConnectedClientAsync(broker);
        ConcurrentQueue<MqttApplicationMessageReceivedEventArgs> received = new();
        MockedMqttClient consumer = await CreateManuallyAcknowledgingClientAsync(
            broker,
            "consumer",
            false,
            received,
            subscriptionQualityOfServiceLevel);
        MqttClientOptions options = consumer.Options!;

        await publisher.PublishAsync(CreateMessage("topic", "1", messageQualityOfServiceLevel));
        await AsyncTestingUtil.WaitAsync(() => !received.IsEmpty);
        await consumer.DisconnectAsync();

        await consumer.ConnectAsync(options);
        await publisher.PublishAsync(CreateMessage("topic", "2", messageQualityOfServiceLevel));
        await AsyncTestingUtil.WaitAsync(() => received.Count >= 2);

        // The redelivered message would precede the newer one, while the unacknowledged deliveries are still counted as pending
        GetPayloads(received).ShouldBe(["1", "2"]);
        broker.GetClientSession("consumer").GetPendingMessagesCount().ShouldBe(2);
    }

    [Fact]
    public async Task Connect_ShouldNotRedeliverUnacknowledgedMessages_WhenSessionIsClean()
    {
        using InMemoryMqttBroker broker = new();
        MockedMqttClient publisher = await CreateConnectedClientAsync(broker);
        ConcurrentQueue<MqttApplicationMessageReceivedEventArgs> received = new();
        MockedMqttClient consumer = await CreateManuallyAcknowledgingClientAsync(broker, "consumer", true, received);
        MqttClientOptions options = consumer.Options!;

        await publisher.PublishAsync(CreateMessage("topic", "1", MqttQualityOfServiceLevel.AtLeastOnce));
        await AsyncTestingUtil.WaitAsync(() => !received.IsEmpty);
        await consumer.DisconnectAsync();

        await consumer.ConnectAsync(options);
        await SubscribeAsync(consumer, "topic", MqttQualityOfServiceLevel.AtLeastOnce);
        await publisher.PublishAsync(CreateMessage("topic", "2", MqttQualityOfServiceLevel.AtLeastOnce));
        await AsyncTestingUtil.WaitAsync(() => received.Count >= 2);

        GetPayloads(received).ShouldBe(["1", "2"]);
        broker.GetClientSession("consumer").GetPendingMessagesCount().ShouldBe(1);
    }

    [Theory]
    [InlineData(MqttQualityOfServiceLevel.AtMostOnce, MqttQualityOfServiceLevel.AtLeastOnce)]
    [InlineData(MqttQualityOfServiceLevel.AtLeastOnce, MqttQualityOfServiceLevel.AtMostOnce)]
    public async Task Connect_ShouldRedeliverUnacknowledgedMessages_WhenAnyOverlappingSubscriptionHasHigherQualityOfService(
        MqttQualityOfServiceLevel firstSubscriptionQualityOfServiceLevel,
        MqttQualityOfServiceLevel secondSubscriptionQualityOfServiceLevel)
    {
        using InMemoryMqttBroker broker = new();
        MockedMqttClient publisher = await CreateConnectedClientAsync(broker);
        ConcurrentQueue<MqttApplicationMessageReceivedEventArgs> received = new();
        MockedMqttClient consumer = await CreateManuallyAcknowledgingClientAsync(
            broker,
            "consumer",
            false,
            received,
            firstSubscriptionQualityOfServiceLevel,
            "topic/#");
        await SubscribeAsync(consumer, "topic/a", secondSubscriptionQualityOfServiceLevel);
        MqttClientOptions options = consumer.Options!;

        await publisher.PublishAsync(CreateMessage("topic/a", "1", MqttQualityOfServiceLevel.ExactlyOnce));
        await AsyncTestingUtil.WaitAsync(() => !received.IsEmpty);
        await consumer.DisconnectAsync();

        await consumer.ConnectAsync(options);
        await AsyncTestingUtil.WaitAsync(() => received.Count >= 2);

        GetPayloads(received).ShouldBe(["1", "1"]);
    }

    private static string[] GetPayloads(IEnumerable<MqttApplicationMessageReceivedEventArgs> received) =>
        [.. received.Select(eventArgs => eventArgs.ApplicationMessage.ConvertPayloadToString())];

    private static async Task<MockedMqttClient> CreateManuallyAcknowledgingClientAsync(
        InMemoryMqttBroker broker,
        string clientId,
        bool cleanSession,
        ConcurrentQueue<MqttApplicationMessageReceivedEventArgs> received,
        MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtLeastOnce,
        string topicFilter = "topic")
    {
        MockedMqttClient client = await CreateClientAsync(broker, clientId, cleanSession);

        client.ApplicationMessageReceivedAsync += args =>
        {
            args.AutoAcknowledge = false;
            received.Enqueue(args);
            return Task.CompletedTask;
        };

        await SubscribeAsync(client, topicFilter, qualityOfServiceLevel);

        return client;
    }

    private static Task SubscribeAsync(MockedMqttClient client, string topicFilter, MqttQualityOfServiceLevel qualityOfServiceLevel) =>
        client.SubscribeAsync(
            new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(topicFilter, qualityOfServiceLevel)
                .Build());

    private static async Task WaitUntilAllMessagesAreConsumedAsync(InMemoryMqttBroker broker)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        await broker.WaitUntilAllMessagesAreConsumedAsync([], timeout.Token);
    }

    private static Task<MockedMqttClient> CreateConnectedClientAsync(InMemoryMqttBroker broker) => CreateClientAsync(broker, "client");

    private static async Task<MockedMqttClient> CreateClientAsync(
        InMemoryMqttBroker broker,
        string clientId,
        bool cleanSession = true,
        string server = "server")
    {
        IMockedMqttOptions mockOptions = Substitute.For<IMockedMqttOptions>();
        mockOptions.ConnectionDelay.Returns(TimeSpan.Zero);

        MockedMqttClient client = new(broker, mockOptions);
        await client.ConnectAsync(
            new MqttClientOptionsBuilder()
                .WithClientId(clientId)
                .WithTcpServer(server)
                .WithCleanSession(cleanSession)
                .Build());

        return client;
    }

    private static Task<MockedMqttClient> CreateSubscribedClientAsync(
        InMemoryMqttBroker broker,
        string clientId,
        ConcurrentQueue<string> received,
        params string[] topicFilters) =>
        CreateSubscribedClientAsync(broker, clientId, received, topicFilters, true);

    private static async Task<MockedMqttClient> CreateSubscribedClientAsync(
        InMemoryMqttBroker broker,
        string clientId,
        ConcurrentQueue<string> received,
        string[] topicFilters,
        bool cleanSession,
        string server = "server")
    {
        MockedMqttClient client = await CreateClientAsync(broker, clientId, cleanSession, server);

        client.ApplicationMessageReceivedAsync += args =>
        {
            received.Enqueue(args.ApplicationMessage.ConvertPayloadToString());
            return Task.CompletedTask;
        };

        MqttClientSubscribeOptionsBuilder optionsBuilder = new();

        foreach (string topicFilter in topicFilters)
        {
            optionsBuilder.WithTopicFilter(topicFilter);
        }

        await client.SubscribeAsync(optionsBuilder.Build());

        return client;
    }

    private static MqttApplicationMessage CreateMessage(
        string topic,
        string payload = "payload",
        MqttQualityOfServiceLevel qualityOfServiceLevel = MqttQualityOfServiceLevel.AtMostOnce) =>
        new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .WithQualityOfServiceLevel(qualityOfServiceLevel)
            .Build();
}
