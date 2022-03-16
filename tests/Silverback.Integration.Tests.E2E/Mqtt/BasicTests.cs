// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Formatter;
using Silverback.Configuration;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public class BasicTests : MqttTestFixture
{
    public BasicTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task OutboundAndInbound_DefaultSettings_ProducedAndConsumed()
    {
        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithClientId("e2e-test").ConnectViaTcp("e2e-mqtt-broker"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(consumer => consumer.ConsumeFrom(DefaultTopicName)))
                .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(15);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(15);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).Content)
            .Should().BeEquivalentTo(Enumerable.Range(1, 15).Select(i => $"{i}"));
    }

    [Fact]
    public async Task OutboundAndInbound_DefaultSettingsV311_ProducedAndConsumed()
    {
        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(
                            configuration => configuration
                                .WithClientId("e2e-test")
                                .ConnectViaTcp("e2e-mqtt-broker")
                                .UseProtocolVersion(MqttProtocolVersion.V311))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound<TestEventOne>(consumer => consumer.ConsumeFrom(DefaultTopicName)))
                .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 15; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(15);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(15);
        Helper.Spy.InboundEnvelopes
            .Select(envelope => ((TestEventOne)envelope.Message!).Content)
            .Should().BeEquivalentTo(Enumerable.Range(1, 15).Select(i => $"{i}"));
    }

    [Fact]
    public async Task OutboundAndInbound_MessageWithCustomHeaders_HeadersTransferred()
    {
        TestEventWithHeaders message = new()
        {
            Content = "Hello E2E!",
            CustomHeader = "Hello header!",
            CustomHeader2 = false
        };

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(configuration => configuration.WithClientId("e2e-test").ConnectViaTcp("e2e-mqtt-broker"))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound(consumer => consumer.ConsumeFrom(DefaultTopicName)))
                .AddIntegrationSpy());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(message);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeEquivalentTo(message);
        Helper.Spy.InboundEnvelopes[0].Headers.Should().ContainEquivalentOf(new MessageHeader("x-custom-header", "Hello header!"));
        Helper.Spy.InboundEnvelopes[0].Headers.Should().ContainEquivalentOf(new MessageHeader("x-custom-header2", "False"));
    }

    [Fact]
    public async Task OutboundAndInbound_MultipleClients_ProducedAndConsumed()
    {
        int client1MessagesCount = 0;
        int client2MessagesCount = 0;

        Host.ConfigureServicesAndRun(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttEndpoints(
                    endpoints => endpoints
                        .ConfigureClient(
                            configuration => configuration
                                .WithClientId("e2e-test")
                                .ConnectViaTcp("e2e-mqtt-broker")
                                .UseProtocolVersion(MqttProtocolVersion.V311))
                        .AddOutbound<IIntegrationEvent>(producer => producer.ProduceTo(DefaultTopicName))
                        .AddInbound<TestEventOne>(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .ConfigureClient(configuration => configuration.WithClientId("client1")))
                        .AddInbound<TestEventOne>(
                            consumer => consumer
                                .ConsumeFrom(DefaultTopicName)
                                .ConfigureClient(configuration => configuration.WithClientId("client2"))))
                .AddDelegateSubscriber2<IRawInboundEnvelope>(HandleEnvelope));

        void HandleEnvelope(IRawInboundEnvelope envelope)
        {
            MqttConsumerConfiguration consumerConfiguration = (MqttConsumerConfiguration)envelope.Endpoint.Configuration;

            switch (consumerConfiguration.Client.ClientId)
            {
                case "client1":
                    client1MessagesCount++;
                    break;
                case "client2":
                    client2MessagesCount++;
                    break;
                default:
                    throw new InvalidOperationException();
            }
        }

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishAsync(new TestEventOne { Content = $"{i}" });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        client1MessagesCount.Should().Be(5);
        client2MessagesCount.Should().Be(5);
    }
}
