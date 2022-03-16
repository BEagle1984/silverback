// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using MQTTnet.Formatter;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public class SerializationTests : MqttTestFixture
{
    public SerializationTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task JsonSerializer_WithHardcodedMessageTypeV311_ProducedAndConsumed()
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
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .SerializeAsJson(serializer => serializer.UseFixedType<TestEventOne>()))
                            .AddInbound<TestEventOne>(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .DeserializeJson()))
                    .AddIntegrationSpy());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
    }

    [Fact]
    public async Task JsonSerializer_WithHardcodedMessageTypeV500_ProducedAndConsumed()
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
                                    .UseProtocolVersion(MqttProtocolVersion.V500))
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .SerializeAsJson(serializer => serializer.UseFixedType<TestEventOne>()))
                            .AddInbound<TestEventOne>(
                                consumer => consumer
                                    .ConsumeFrom(DefaultTopicName)
                                    .DeserializeJson()))
                    .AddIntegrationSpy());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.InboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
    }
}
