// Copyright (c) 2024 Sergio Aquilini
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
    public async Task SetMessageId_ShouldAddHeaderFromMessage()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => LoggingServiceCollectionExtensions.AddLogging(services)
                .AddSilverback()
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
                                        .SetMessageId<TestEventOne>(message => message?.ContentEventOne))))
                .AddIntegrationSpy());

        IPublisher publisher = ServiceProviderServiceExtensions.GetRequiredService<IPublisher>(Host.ScopedServiceProvider);
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "two" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "three" });
        await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = "four" });

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(4);
        Helper.Spy.OutboundEnvelopes[0].Headers.Should().ContainSingle(header => header.Name == "x-message-id" && header.Value == "one");
        Helper.Spy.OutboundEnvelopes[1].Headers.Should().ContainSingle(header => header.Name == "x-message-id" && header.Value == "two");
        Helper.Spy.OutboundEnvelopes[2].Headers.Should().ContainSingle(header => header.Name == "x-message-id" && header.Value == "three");
        Helper.Spy.OutboundEnvelopes[3].Headers.Should().NotContain(header => header.Name == "x-message-id");
    }

    [Fact]
    public async Task SetMessageId_ShouldAddHeaderFromEnvelope()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => LoggingServiceCollectionExtensions.AddLogging(services)
                .AddSilverback()
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
                                        .SetMessageId<TestEventOne>(envelope => envelope.Message?.ContentEventOne))))
                .AddIntegrationSpy());

        IPublisher publisher = ServiceProviderServiceExtensions.GetRequiredService<IPublisher>(Host.ScopedServiceProvider);
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "two" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "three" });
        await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = "four" });

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(4);
        Helper.Spy.OutboundEnvelopes[0].Headers.Should().ContainSingle(header => header.Name == "x-message-id" && header.Value == "one");
        Helper.Spy.OutboundEnvelopes[1].Headers.Should().ContainSingle(header => header.Name == "x-message-id" && header.Value == "two");
        Helper.Spy.OutboundEnvelopes[2].Headers.Should().ContainSingle(header => header.Name == "x-message-id" && header.Value == "three");
        Helper.Spy.OutboundEnvelopes[3].Headers.Should().NotContain(header => header.Name == "x-message-id");
    }

    [Fact]
    public async Task SetMessageId_ShouldAddHeaderByMessageType()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => LoggingServiceCollectionExtensions.AddLogging(services)
                .AddSilverback()
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
                                        .SetMessageId<TestEventOne>(message => message?.ContentEventOne)
                                        .SetMessageId((TestEventTwo? _) => "two"))))
                .AddIntegrationSpy());

        IPublisher publisher = ServiceProviderServiceExtensions.GetRequiredService<IPublisher>(Host.ScopedServiceProvider);
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = "two" });
        await publisher.PublishEventAsync(new TestEventThree { ContentEventThree = "three" });

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.OutboundEnvelopes[0].Headers.Should().ContainSingle(header => header.Name == "x-message-id" && header.Value == "one");
        Helper.Spy.OutboundEnvelopes[1].Headers.Should().ContainSingle(header => header.Name == "x-message-id" && header.Value == "two");
        Helper.Spy.OutboundEnvelopes[2].Headers.Should().NotContain(header => header.Name == "x-message-id");
    }
}
