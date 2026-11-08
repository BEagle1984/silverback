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

    private static MqttApplicationMessage CreateMessage(string topic, string payload = "payload") =>
        new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload(payload)
            .Build();
}
