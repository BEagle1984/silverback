// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Formatter;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt
{
    public class SerializationTests : MqttTestFixture
    {
        public SerializationTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task JsonSerializer_WithHardcodedMessageTypeV311_ProducedAndConsumed()
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
                                        .ConnectViaTcp("e2e-mqtt-broker")
                                        .UseProtocolVersion(MqttProtocolVersion.V311))
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .SerializeAsJson(
                                            serializer => serializer.UseFixedType<TestEventOne>()))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .DeserializeJson(
                                            serializer => serializer.UseFixedType<TestEventOne>())))
                        .AddIntegrationSpy())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne());
            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.OutboundEnvelopes[0].Headers.Should()
                .NotContain(header => header.Name == DefaultMessageHeaders.MessageType);
            Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
        }

        [Fact]
        public async Task JsonSerializer_WithHardcodedMessageTypeV500_ProducedAndConsumed()
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
                                        .ConnectViaTcp("e2e-mqtt-broker")
                                        .UseProtocolVersion(MqttProtocolVersion.V500))
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .SerializeAsJson(
                                            serializer => serializer.UseFixedType<TestEventOne>()))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .DeserializeJson(
                                            serializer => serializer.UseFixedType<TestEventOne>())))
                        .AddIntegrationSpy())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne());
            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.OutboundEnvelopes[0].Headers.Should()
                .NotContain(header => header.Name == DefaultMessageHeaders.MessageType);
            Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
        }

        [Fact]
        public async Task JsonSerializer_WithHardcodedMessageType_MessageTypeHeaderIgnored()
        {
            var message = new TestEventOne { Content = "Hello E2E!" };
            byte[] rawMessage = (await Endpoint.DefaultSerializer.SerializeAsync(
                                    message,
                                    new MessageHeaderCollection(),
                                    MessageSerializationContext.Empty)).ReadAll() ??
                                throw new InvalidOperationException("Serializer returned null");

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
                                        .ConnectViaTcp("e2e-mqtt-broker")
                                        .UseProtocolVersion(MqttProtocolVersion.V500))
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .SerializeAsJson(
                                            serializer => serializer.UseFixedType<TestEventOne>()))
                                .AddInbound(
                                    endpoint => endpoint
                                        .ConsumeFrom(DefaultTopicName)
                                        .DeserializeJson(
                                            serializer => serializer.UseFixedType<TestEventOne>())))
                        .AddIntegrationSpy())
                .Run();

            var producer = Helper.Broker.GetProducer(
                new MqttProducerEndpoint(DefaultTopicName)
                {
                    Configuration =
                        ((MqttClientConfigBuilder)new MqttClientConfigBuilder()
                            .WithClientId("e2e-test-helper")
                            .ConnectViaTcp("e2e-mqtt-broker")
                            .UseProtocolVersion(MqttProtocolVersion.V500))
                        .Build()
                });
            await producer.RawProduceAsync(
                rawMessage,
                new MessageHeaderCollection
                {
                    { DefaultMessageHeaders.MessageType, "Silverback.Bad.TestEventOne, Silverback.Bad" }
                });

            await Helper.WaitUntilAllMessagesAreConsumedAsync();

            Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
        }
    }
}
