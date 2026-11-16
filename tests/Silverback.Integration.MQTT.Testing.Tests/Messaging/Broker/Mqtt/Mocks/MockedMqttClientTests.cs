// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet;
using MQTTnet.Exceptions;
using MQTTnet.Protocol;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker.Mqtt.Mocks;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Testing.Messaging.Broker.Mqtt.Mocks;

public class MockedMqttClientTests
{
    [Fact]
    public async Task SubscribeAsync_ShouldGrantRequestedQualityOfService()
    {
        using InMemoryMqttBroker broker = new();
        using MockedMqttClient client = await CreateConnectedClientAsync(broker);

        MqttClientSubscribeResult result = await client.SubscribeAsync(
            new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter("topic1")
                .WithTopicFilter("topic2", MqttQualityOfServiceLevel.AtLeastOnce)
                .WithTopicFilter("topic3", MqttQualityOfServiceLevel.ExactlyOnce)
                .Build());

        result.Items.Select(item => item.ResultCode).ShouldBe(
        [
            MqttClientSubscribeResultCode.GrantedQoS0,
            MqttClientSubscribeResultCode.GrantedQoS1,
            MqttClientSubscribeResultCode.GrantedQoS2
        ]);
    }

    // Like the real client, which leaves the other invalid filters (e.g. 'topic/sub#') to the broker
    [Theory]
    [InlineData("topic/#/sub")]
    [InlineData("#/sub")]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The action is awaited before disposing")]
    public async Task SubscribeAsync_ShouldThrow_WhenMultiLevelWildcardIsNotLast(string topicFilter)
    {
        using InMemoryMqttBroker broker = new();
        using MockedMqttClient client = await CreateConnectedClientAsync(broker);

        Func<Task> act = () => client.SubscribeAsync(
            new MqttClientSubscribeOptionsBuilder()
                .WithTopicFilter(topicFilter)
                .Build());

        await act.ShouldThrowAsync<MqttProtocolViolationException>();
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "The action is awaited before disposing")]
    public async Task ConnectAsync_ShouldLeaveClientDisconnectedWithoutSession_WhenConnectionDelayCanceled()
    {
        using InMemoryMqttBroker broker = new();
        IMockedMqttOptions mockOptions = Substitute.For<IMockedMqttOptions>();
        mockOptions.ConnectionDelay.Returns(TimeSpan.FromHours(1));
        using MockedMqttClient client = new(broker, mockOptions);
        using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromMilliseconds(50));

        Func<Task> act = () => client.ConnectAsync(CreateOptions(), cancellationTokenSource.Token);

        await act.ShouldThrowAsync<OperationCanceledException>();
        client.IsConnected.ShouldBeFalse();
        Should.Throw<InvalidOperationException>(() => broker.GetClientSession("client"));
    }

    [Fact]
    public async Task ConnectAsync_ShouldConnect_WhenPreviousConnectionDelayWasCanceled()
    {
        using InMemoryMqttBroker broker = new();
        IMockedMqttOptions mockOptions = Substitute.For<IMockedMqttOptions>();
        mockOptions.ConnectionDelay.Returns(TimeSpan.FromHours(1));
        using MockedMqttClient client = new(broker, mockOptions);
        using CancellationTokenSource cancellationTokenSource = new(TimeSpan.FromMilliseconds(50));

        try
        {
            await client.ConnectAsync(CreateOptions(), cancellationTokenSource.Token);
        }
        catch (OperationCanceledException)
        {
            // Expected
        }

        mockOptions.ConnectionDelay.Returns(TimeSpan.Zero);
        await client.ConnectAsync(CreateOptions());

        client.IsConnected.ShouldBeTrue();
        broker.GetClientSession("client").ShouldNotBeNull();
    }

    private static MqttClientOptions CreateOptions() =>
        new MqttClientOptionsBuilder()
            .WithClientId("client")
            .WithTcpServer("server")
            .Build();

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
}
