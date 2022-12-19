// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using MQTTnet.Client;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Mqtt;
using Silverback.Messaging.Configuration.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Broker
{
    public class MqttClientWrapperTests
    {
        // TODO: How to implement?
        [Fact(Skip = "To be reimplemented")]
        public Task ConnectAsync_CalledFromMultipleSenders_ClientConnectedOnce()
        {
            throw new NotImplementedException();

            // var sender1 = new object();
            // var sender2 = new object();
            // var mqttClientConfig = new MqttClientConfig();
            //
            // var mqttClient = Substitute.For<IMqttClient>();
            // mqttClient.ConnectAsync(Arg.Any<IMqttClientOptions>(), Arg.Any<CancellationToken>())
            //     .ReturnsForAnyArgs(_ => Task.FromResult(new MqttClientAuthenticateResult()));
            //
            // var clientWrapper = new MqttClientWrapper(
            //     mqttClient,
            //     mqttClientConfig,
            //     Substitute.For<IBrokerCallbacksInvoker>(),
            //     Substitute.For<ISilverbackLogger>());
            //
            // var task1 = clientWrapper.ConnectAsync(sender1);
            // var task2 = clientWrapper.ConnectAsync(sender2);
            //
            // task2.Should().BeSameAs(task1);
            //
            // await task1;
            //
            // await mqttClient.Received(1).ConnectAsync(
            //     Arg.Any<IMqttClientOptions>(),
            //     Arg.Any<CancellationToken>());
            // await mqttClient.Received(1).ConnectAsync(
            //     mqttClientConfig.GetMqttClientOptions(),
            //     Arg.Any<CancellationToken>());
        }

        // TODO: How to implement?
        [Fact(Skip = "To be reimplemented")]
        public Task ConnectAsync_CalledWhenAlreadyConnected_ClientConnectNotCalled()
        {
            throw new NotImplementedException();

            // var mqttClient = Substitute.For<IMqttClient>();
            // mqttClient.ConnectAsync(Arg.Any<IMqttClientOptions>(), Arg.Any<CancellationToken>())
            //     .ReturnsForAnyArgs(_ => Task.FromResult(new MqttClientAuthenticateResult()));
            // mqttClient.IsConnected.Returns(true);
            //
            // var clientWrapper = new MqttClientWrapper(
            //     mqttClient,
            //     new MqttClientConfig(),
            //     Substitute.For<ILogger>());
            //
            // await clientWrapper.ConnectAsync(new object());
            //
            // await mqttClient.Received(0).ConnectAsync(
            //     Arg.Any<IMqttClientOptions>(),
            //     Arg.Any<CancellationToken>());
        }

        [Fact]
        public async Task
            DisconnectAsync_CalledFromMultipleSenders_ClientDisconnectedWhenAllSendersDisconnect()
        {
            var sender1 = new object();
            var sender2 = new object();
            var mqttClientConfig = new MqttClientConfig();

            var mqttClient = Substitute.For<IMqttClient>();
            mqttClient.ConnectAsync(Arg.Any<MqttClientOptions>(), Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(_ => Task.FromResult(new MqttClientConnectResult()));

            var clientWrapper = new MqttClientWrapper(
                mqttClient,
                mqttClientConfig,
                Substitute.For<IBrokerCallbacksInvoker>(),
                Substitute.For<ISilverbackLogger>());

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
            mqttClient.ConnectAsync(Arg.Any<MqttClientOptions>(), Arg.Any<CancellationToken>())
                .ReturnsForAnyArgs(_ => Task.FromResult(new MqttClientConnectResult()));
            mqttClient.IsConnected.Returns(false);

            var clientWrapper = new MqttClientWrapper(
                mqttClient,
                new MqttClientConfig(),
                Substitute.For<IBrokerCallbacksInvoker>(),
                Substitute.For<ISilverbackLogger>());

            await clientWrapper.DisconnectAsync(new object());

            await mqttClient.Received(0).DisconnectAsync(
                Arg.Any<MqttClientDisconnectOptions>(),
                Arg.Any<CancellationToken>());
        }
    }
}
