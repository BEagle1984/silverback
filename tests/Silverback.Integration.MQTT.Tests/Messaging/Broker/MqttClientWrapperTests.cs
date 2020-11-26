// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Xunit;

namespace Silverback.Tests.Integration.MQTT.Messaging.Broker
{
    public class MqttClientWrapperTests
    {
        [Fact]
        public void ConnectAsync_CalledFromMultipleSenders_ClientConnectedOnce()
        {
            var sender1 = new object();
            var sender2 = new object();
            var mqttClientConfig = new MqttClientConfig();

            var mqttClient = Substitute.For<IMqttClient>();
            mqttClient.ConnectAsync(Arg.Any<IMqttClientOptions>(), Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(_ => Task.FromResult(new MqttClientAuthenticateResult()));

            var clientWrapper = new MqttClientWrapper(mqttClient, mqttClientConfig);

            var task1 = clientWrapper.ConnectAsync(sender1);
            var task2 = clientWrapper.ConnectAsync(sender2);

            task2.Should().BeSameAs(task1);
            mqttClient.Received(1).ConnectAsync(Arg.Any<IMqttClientOptions>(), Arg.Any<CancellationToken>());
            mqttClient.Received(1).ConnectAsync(mqttClientConfig.GetMqttClientOptions(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task ConnectAsync_CalledWhenAlreadyConnected_ClientConnectNotCalled()
        {
            var mqttClient = Substitute.For<IMqttClient>();
            mqttClient.ConnectAsync(Arg.Any<IMqttClientOptions>(), Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(_ => Task.FromResult(new MqttClientAuthenticateResult()));
            mqttClient.IsConnected.Returns(true);

            var clientWrapper = new MqttClientWrapper(mqttClient, new MqttClientConfig());

            await clientWrapper.ConnectAsync(new object());

            await mqttClient.Received(0).ConnectAsync(Arg.Any<IMqttClientOptions>(), Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DisconnectAsync_CalledFromMultipleSenders_ClientDisconnectedWhenAllSendersDisconnect()
        {
            var sender1 = new object();
            var sender2 = new object();
            var mqttClientConfig = new MqttClientConfig();

            var mqttClient = Substitute.For<IMqttClient>();
            mqttClient.ConnectAsync(Arg.Any<IMqttClientOptions>(), Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(_ => Task.FromResult(new MqttClientAuthenticateResult()));

            var clientWrapper = new MqttClientWrapper(mqttClient, mqttClientConfig);

            await clientWrapper.ConnectAsync(sender1);
            await clientWrapper.ConnectAsync(sender2);

            mqttClient.IsConnected.Returns(true);

            await clientWrapper.DisconnectAsync(sender1);

            await mqttClient.Received(0).DisconnectAsync(
                Arg.Any<MqttClientDisconnectOptions>(),
                Arg.Any<CancellationToken>());

            await clientWrapper.DisconnectAsync(sender2);

            await mqttClient.Received(1).DisconnectAsync(
                Arg.Any<MqttClientDisconnectOptions>(),
                Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task DisconnectAsync_CalledWhenNotConnected_ClientDisconnectNotCalled()
        {
            var mqttClient = Substitute.For<IMqttClient>();
            mqttClient.ConnectAsync(Arg.Any<IMqttClientOptions>(), Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(_ => Task.FromResult(new MqttClientAuthenticateResult()));
            mqttClient.IsConnected.Returns(false);

            var clientWrapper = new MqttClientWrapper(mqttClient, new MqttClientConfig());

            await clientWrapper.DisconnectAsync(new object());

            await mqttClient.Received(0).DisconnectAsync(
                Arg.Any<MqttClientDisconnectOptions>(),
                Arg.Any<CancellationToken>());
        }
    }
}
