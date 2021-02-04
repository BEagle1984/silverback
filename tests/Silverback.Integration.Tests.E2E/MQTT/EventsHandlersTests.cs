// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt
{
    public class EventsHandlersTests : MqttTestFixture
    {
        public EventsHandlersTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public void OnConnected_DefaultSettings_CallbackCalled()
        {
            int callbackCalls = 0;
            Func<MqttClientConfig, Task> callback = _ =>
            {
                Interlocked.Increment(ref callbackCalls);
                return Task.CompletedTask;
            };

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
                                .BindEvents(b => b.OnConnected(callback)) // OnConnected Callback
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            callbackCalls.Should().Be(1);
        }

        [Fact]
        public async Task OnConnected_DefaultSettings_MessageSent()
        {
            TestEventOne message = new();

            Func<MqttClientConfig, Task> callback = async _ =>
            {
                var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
                await publisher.PublishAsync(message);
            };

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
                                .BindEvents(b => b.OnConnected(callback)) // OnConnected Callback
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            await Task.Delay(TimeSpan.FromSeconds(2));

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.OutboundEnvelopes[0].Message.Should().BeSameAs(message);
        }

        [Fact]
        public async Task OnDisconnecting_DefaultSettings_CallbackCalled()
        {
            int callbackCalls = 0;
            Func<MqttClientConfig, Task> callback = _ =>
            {
                Interlocked.Increment(ref callbackCalls);
                return Task.CompletedTask;
            };

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
                                .BindEvents(b => b.OnDisconnecting(callback)) // OnDisconnecting Callback
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var brokers = Host.ServiceProvider.GetRequiredService<IBrokerCollection>();
            await brokers.DisconnectAsync();

            callbackCalls.Should().Be(1);
        }

        [Fact]
        public async Task OnDisconnecting_DefaultSettings_MessageSent()
        {
            TestEventOne message = new();

            Func<MqttClientConfig, Task> callback = async _ =>
            {
                var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
                await publisher.PublishAsync(message);
            };

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
                                .BindEvents(b => b.OnConnected(callback)) // OnDisconnecting Callback
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var brokers = Host.ServiceProvider.GetRequiredService<IBrokerCollection>();

            await brokers.DisconnectAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.OutboundEnvelopes[0].Message.Should().BeSameAs(message);
        }
    }
}
