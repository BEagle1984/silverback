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

        IPublisher publisher = ServiceProviderServiceExtensions.GetRequiredService<IPublisher>(Host.ServiceProvider);
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "two" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "three" });
        await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = "four" });

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(4);
        Helper.Spy.OutboundEnvelopes[0].Headers.ShouldContain(header => header.Name == "x-message-id" && header.Value == "one");
        Helper.Spy.OutboundEnvelopes[1].Headers.ShouldContain(header => header.Name == "x-message-id" && header.Value == "two");
        Helper.Spy.OutboundEnvelopes[2].Headers.ShouldContain(header => header.Name == "x-message-id" && header.Value == "three");
        Helper.Spy.OutboundEnvelopes[3].Headers.ShouldNotContain(header => header.Name == "x-message-id");
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

        IPublisher publisher = ServiceProviderServiceExtensions.GetRequiredService<IPublisher>(Host.ServiceProvider);
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "two" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "three" });
        await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = "four" });

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(4);
        Helper.Spy.OutboundEnvelopes[0].Headers.ShouldContain(header => header.Name == "x-message-id" && header.Value == "one");
        Helper.Spy.OutboundEnvelopes[1].Headers.ShouldContain(header => header.Name == "x-message-id" && header.Value == "two");
        Helper.Spy.OutboundEnvelopes[2].Headers.ShouldContain(header => header.Name == "x-message-id" && header.Value == "three");
        Helper.Spy.OutboundEnvelopes[3].Headers.ShouldNotContain(header => header.Name == "x-message-id");
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

        IPublisher publisher = ServiceProviderServiceExtensions.GetRequiredService<IPublisher>(Host.ServiceProvider);
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = "two" });
        await publisher.PublishEventAsync(new TestEventThree { ContentEventThree = "three" });

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.OutboundEnvelopes[0].Headers.ShouldContain(header => header.Name == "x-message-id" && header.Value == "one");
        Helper.Spy.OutboundEnvelopes[1].Headers.ShouldContain(header => header.Name == "x-message-id" && header.Value == "two");
        Helper.Spy.OutboundEnvelopes[2].Headers.ShouldNotContain(header => header.Name == "x-message-id");
    }
}
