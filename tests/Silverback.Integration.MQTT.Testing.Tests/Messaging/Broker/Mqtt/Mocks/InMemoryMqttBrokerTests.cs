// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
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

        Task producer = Task.Run(
            async () =>
            {
                for (int i = 0; i < 10_000 && !cancellationTokenSource.IsCancellationRequested; i++)
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

    private static async Task<MockedMqttClient> CreateConnectedClientAsync(InMemoryMqttBroker broker)
    {
        IMockedMqttOptions mockOptions = Substitute.For<IMockedMqttOptions>();
        mockOptions.ConnectionDelay.Returns(TimeSpan.Zero);

        MockedMqttClient client = new(broker, mockOptions);
        await client.ConnectAsync(
            new MqttClientOptionsBuilder()
                .WithClientId("client")
                .WithTcpServer("server")
                .Build());

        return client;
    }

    private static MqttApplicationMessage CreateMessage(string topic) =>
        new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithPayload("payload")
            .Build();
}
