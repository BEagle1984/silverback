// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class OutboundMessageEnrichmentFixture
{
    [Fact]
    public async Task WithMessageId_ShouldAddHeaderFromMessage()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => LoggingServiceCollectionExtensions.AddLogging(services)
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Produce<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .WithMessageId<TestEventOne>(message => message?.ContentEventOne))))
                .AddIntegrationSpy());

        IEventPublisher publisher = ServiceProviderServiceExtensions.GetRequiredService<IEventPublisher>(Host.ScopedServiceProvider);
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "two" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "three" });
        await publisher.PublishAsync(new TestEventTwo { ContentEventTwo = "four" });

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(4);
        Helper.Spy.OutboundEnvelopes[0].Headers.Should().ContainSingle(header => header.Name == "x-message-id" && header.Value == "one");
        Helper.Spy.OutboundEnvelopes[1].Headers.Should().ContainSingle(header => header.Name == "x-message-id" && header.Value == "two");
        Helper.Spy.OutboundEnvelopes[2].Headers.Should().ContainSingle(header => header.Name == "x-message-id" && header.Value == "three");
        Helper.Spy.OutboundEnvelopes[3].Headers.Should().NotContain(header => header.Name == "x-message-id");
    }

    [Fact]
    public async Task WithMessageId_ShouldAddHeaderFromEnvelope()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => LoggingServiceCollectionExtensions.AddLogging(services)
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Produce<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .WithMessageId<TestEventOne>(envelope => envelope.Message?.ContentEventOne))))
                .AddIntegrationSpy());

        IEventPublisher publisher = ServiceProviderServiceExtensions.GetRequiredService<IEventPublisher>(Host.ScopedServiceProvider);
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "two" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "three" });
        await publisher.PublishAsync(new TestEventTwo { ContentEventTwo = "four" });

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(4);
        Helper.Spy.OutboundEnvelopes[0].Headers.Should().ContainSingle(header => header.Name == "x-message-id" && header.Value == "one");
        Helper.Spy.OutboundEnvelopes[1].Headers.Should().ContainSingle(header => header.Name == "x-message-id" && header.Value == "two");
        Helper.Spy.OutboundEnvelopes[2].Headers.Should().ContainSingle(header => header.Name == "x-message-id" && header.Value == "three");
        Helper.Spy.OutboundEnvelopes[3].Headers.Should().NotContain(header => header.Name == "x-message-id");
    }

    [Fact]
    public async Task WithMessageId_ShouldAddHeaderByMessageType()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => LoggingServiceCollectionExtensions.AddLogging(services)
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Produce<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .WithMessageId<TestEventOne>(message => message?.ContentEventOne)
                                        .WithMessageId((TestEventTwo? _) => "two"))))
                .AddIntegrationSpy());

        IEventPublisher publisher = ServiceProviderServiceExtensions.GetRequiredService<IEventPublisher>(Host.ScopedServiceProvider);
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishAsync(new TestEventTwo { ContentEventTwo = "two" });
        await publisher.PublishAsync(new TestEventThree { ContentEventThree = "three" });

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.OutboundEnvelopes[0].Headers.Should().ContainSingle(header => header.Name == "x-message-id" && header.Value == "one");
        Helper.Spy.OutboundEnvelopes[1].Headers.Should().ContainSingle(header => header.Name == "x-message-id" && header.Value == "two");
        Helper.Spy.OutboundEnvelopes[2].Headers.Should().NotContain(header => header.Name == "x-message-id");
    }
}
