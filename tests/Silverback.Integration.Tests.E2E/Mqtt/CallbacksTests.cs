// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt
{
    public class CallbacksTests : MqttTestFixture
    {
        public CallbacksTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void OnConnected_DefaultSettings_CallbackInvoked()
        {
            var callbackHandler = new FakeConnectedCallbackHandler();

            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                        .AddMqttEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config => config
                                        .WithClientId("e2e-test")
                                        .ConnectViaTcp("e2e-mqtt-broker"))
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)))
                        .AddSingletonBrokerCallbackHandler(callbackHandler)
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            callbackHandler.CallsCount.Should().Be(1);
        }

        [Fact]
        public async Task OnConnected_SendingMessage_MessageSent()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                        .AddMqttEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config => config
                                        .WithClientId("e2e-test")
                                        .ConnectViaTcp("e2e-mqtt-broker"))
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)))
                        .AddScopedBrokerCallbackHandler<SendMessageConnectedCallbackHandler>()
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            await AsyncTestingUtil.WaitAsync(() => Helper.Spy.OutboundEnvelopes.Count >= 1);

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.OutboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
        }

        [Fact]
        public async Task OnDisconnecting_DefaultSettings_CallbackInvoked()
        {
            var callbackHandler = new FakeDisconnectingCallbackHandler();
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                        .AddMqttEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config => config
                                        .WithClientId("e2e-test")
                                        .ConnectViaTcp("e2e-mqtt-broker"))
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)))
                        .AddSingletonBrokerCallbackHandler(callbackHandler)
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            callbackHandler.CallsCount.Should().Be(0);

            var brokers = Host.ServiceProvider.GetRequiredService<IBrokerCollection>();
            await brokers.DisconnectAsync();

            callbackHandler.CallsCount.Should().Be(1);
        }

        [Fact]
        public async Task OnDisconnecting_SendingMessage_MessageSent()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                        .AddMqttEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config => config
                                        .WithClientId("e2e-test")
                                        .ConnectViaTcp("e2e-mqtt-broker"))
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)))
                        .AddScopedBrokerCallbackHandler<SendMessageDisconnectingCallbackHandler>()
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var brokers = Host.ServiceProvider.GetRequiredService<IBrokerCollection>();
            await brokers.DisconnectAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.OutboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
        }

        [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
        private class FakeConnectedCallbackHandler : IMqttClientConnectedCallback
        {
            public int CallsCount { get; private set; }

            public Task OnClientConnectedAsync(MqttClientConfig config)
            {
                CallsCount++;
                return Task.CompletedTask;
            }
        }

        [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
        private class SendMessageConnectedCallbackHandler : IMqttClientConnectedCallback
        {
            private readonly IPublisher _publisher;

            public SendMessageConnectedCallbackHandler(IPublisher publisher)
            {
                _publisher = publisher;
            }

            public Task OnClientConnectedAsync(MqttClientConfig config) =>
                _publisher.PublishAsync(new TestEventOne());
        }

        [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
        private class FakeDisconnectingCallbackHandler : IMqttClientDisconnectingCallback
        {
            public int CallsCount { get; private set; }

            public Task OnClientDisconnectingAsync(MqttClientConfig config)
            {
                CallsCount++;
                return Task.CompletedTask;
            }
        }

        [SuppressMessage("", "CA1812", Justification = "Class used via DI")]
        private class SendMessageDisconnectingCallbackHandler : IMqttClientDisconnectingCallback
        {
            private readonly IPublisher _publisher;

            public SendMessageDisconnectingCallbackHandler(IPublisher publisher)
            {
                _publisher = publisher;
            }

            public Task OnClientDisconnectingAsync(MqttClientConfig config) =>
                _publisher.PublishAsync(new TestEventOne());
        }
    }
}
