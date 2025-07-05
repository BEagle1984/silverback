// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
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
    public async Task AddHeader_ShouldAddHeaderFromMessage()
    {
        await Host.ConfigureServicesAndRunAsync(services => LoggingServiceCollectionExtensions.AddLogging(services)
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<IIntegrationEvent>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .AddHeader<TestEventOne>("header1", message => message?.ContentEventOne))))
            .AddIntegrationSpy());

        IPublisher publisher = ServiceProviderServiceExtensions.GetRequiredService<IPublisher>(Host.ServiceProvider);
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "two" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "three" });
        await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = "four" });

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(4);
        Helper.Spy.OutboundEnvelopes[0].Headers.ShouldContain(header => header.Name == "header1" && header.Value == "one");
        Helper.Spy.OutboundEnvelopes[1].Headers.ShouldContain(header => header.Name == "header1" && header.Value == "two");
        Helper.Spy.OutboundEnvelopes[2].Headers.ShouldContain(header => header.Name == "header1" && header.Value == "three");
        Helper.Spy.OutboundEnvelopes[3].Headers.ShouldNotContain(header => header.Name == "header1");
    }

    [Fact]
    public async Task AddHeader_ShouldAddHeaderFromEnvelope()
    {
        await Host.ConfigureServicesAndRunAsync(services => LoggingServiceCollectionExtensions.AddLogging(services)
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<IIntegrationEvent>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .AddHeader<TestEventOne>("header1", envelope => envelope.Message?.ContentEventOne))))
            .AddIntegrationSpy());

        IPublisher publisher = ServiceProviderServiceExtensions.GetRequiredService<IPublisher>(Host.ServiceProvider);
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "two" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "three" });
        await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = "four" });

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(4);
        Helper.Spy.OutboundEnvelopes[0].Headers.ShouldContain(header => header.Name == "header1" && header.Value == "one");
        Helper.Spy.OutboundEnvelopes[1].Headers.ShouldContain(header => header.Name == "header1" && header.Value == "two");
        Helper.Spy.OutboundEnvelopes[2].Headers.ShouldContain(header => header.Name == "header1" && header.Value == "three");
        Helper.Spy.OutboundEnvelopes[3].Headers.ShouldNotContain(header => header.Name == "header1");
    }

    [Fact]
    public async Task AddHeader_ShouldAddHeaderByMessageType()
    {
        await Host.ConfigureServicesAndRunAsync(services => LoggingServiceCollectionExtensions.AddLogging(services)
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
            .AddMqttClients(clients => clients
                .ConnectViaTcp("e2e-mqtt-broker")
                .AddClient(client => client
                    .WithClientId(DefaultClientId)
                    .Produce<IIntegrationEvent>(endpoint => endpoint
                        .ProduceTo(DefaultTopicName)
                        .AddHeader<TestEventOne>("header1", message => message?.ContentEventOne)
                        .AddHeader("header2", (TestEventTwo? _) => "two"))))
            .AddIntegrationSpy());

        IPublisher publisher = ServiceProviderServiceExtensions.GetRequiredService<IPublisher>(Host.ServiceProvider);
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = "two" });
        await publisher.PublishEventAsync(new TestEventThree { ContentEventThree = "three" });

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.OutboundEnvelopes[0].Headers.ShouldContain(header => header.Name == "header1" && header.Value == "one");
        Helper.Spy.OutboundEnvelopes[1].Headers.ShouldContain(header => header.Name == "header2" && header.Value == "two");
        Helper.Spy.OutboundEnvelopes[2].Headers.ShouldNotContain(header => header.Name == "header1");
        Helper.Spy.OutboundEnvelopes[2].Headers.ShouldNotContain(header => header.Name == "header2");
    }
}
