// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Mqtt;

public partial class ConsumerEndpointFixture
{
    [Fact]
    public async Task ConsumerEndpoint_ShouldConsume_WhenSubscribedUsingSingleLevelWildcard()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Consume(endpoint => endpoint.ConsumeFrom("world/europe/switzerland/+"))
                                .Consume(endpoint => endpoint.ConsumeFrom("world/america/+/news"))))
                .AddIntegrationSpyAndSubscriber());

        IProducer GetProducer(string topic) => Helper.GetProducer(
            client => client
                .ConnectViaTcp("e2e-mqtt-broker")
                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(topic)));

        await GetProducer("world/europe/switzerland/graubuenden/mesocco")
            .ProduceAsync(new TestEventOne { ContentEventOne = "Mesocco" });
        await GetProducer("world/europe/switzerland/ticino")
            .ProduceAsync(new TestEventOne { ContentEventOne = "Ticino" });
        await GetProducer("world/europe/switzerland")
            .ProduceAsync(new TestEventOne { ContentEventOne = "Switzerland" });
        await GetProducer("world/europe/germany")
            .ProduceAsync(new TestEventOne { ContentEventOne = "Germany" });
        await GetProducer("world/asia")
            .ProduceAsync(new TestEventOne { ContentEventOne = "Asia" });
        await GetProducer("world/america")
            .ProduceAsync(new TestEventOne { ContentEventOne = "America" });
        await GetProducer("world/america/usa")
            .ProduceAsync(new TestEventOne { ContentEventOne = "USA" });
        await GetProducer("world/america/usa/news")
            .ProduceAsync(new TestEventOne { ContentEventOne = "USA News" });
        await GetProducer("world/america/usa/california/news")
            .ProduceAsync(new TestEventOne { ContentEventOne = "California News" });
        await GetProducer("world/america/canada")
            .ProduceAsync(new TestEventOne { ContentEventOne = "Canada" });
        await GetProducer("world/america/canada/news")
            .ProduceAsync(new TestEventOne { ContentEventOne = "Canada News" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Select(envelope => envelope.Message.As<TestEventOne>().ContentEventOne)
            .Should().BeEquivalentTo("Ticino", "USA News", "Canada News");
    }

    [Fact]
    public async Task ConsumerEndpoint_ShouldConsume_WhenSubscribedUsingMultiLevelWildcard()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedMqtt())
                .AddMqttClients(
                    clients => clients
                        .ConnectViaTcp("e2e-mqtt-broker")
                        .AddClient(
                            client => client
                                .WithClientId(DefaultClientId)
                                .Consume(endpoint => endpoint.ConsumeFrom("world/europe/switzerland/#"))
                                .Consume(endpoint => endpoint.ConsumeFrom("world/america/#/news"))))
                .AddIntegrationSpyAndSubscriber());

        IProducer GetProducer(string topic) => Helper.GetProducer(
            client => client
                .ConnectViaTcp("e2e-mqtt-broker")
                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(topic)));

        await GetProducer("world/europe/switzerland/graubuenden/mesocco")
            .ProduceAsync(new TestEventOne { ContentEventOne = "Mesocco" });
        await GetProducer("world/europe/switzerland/ticino")
            .ProduceAsync(new TestEventOne { ContentEventOne = "Ticino" });
        await GetProducer("world/europe/switzerland")
            .ProduceAsync(new TestEventOne { ContentEventOne = "Switzerland" });
        await GetProducer("world/europe/germany")
            .ProduceAsync(new TestEventOne { ContentEventOne = "Germany" });
        await GetProducer("world/asia")
            .ProduceAsync(new TestEventOne { ContentEventOne = "Asia" });
        await GetProducer("world/america")
            .ProduceAsync(new TestEventOne { ContentEventOne = "America" });
        await GetProducer("world/america/usa")
            .ProduceAsync(new TestEventOne { ContentEventOne = "USA" });
        await GetProducer("world/america/usa/news")
            .ProduceAsync(new TestEventOne { ContentEventOne = "USA News" });
        await GetProducer("world/america/usa/california/news")
            .ProduceAsync(new TestEventOne { ContentEventOne = "California News" });
        await GetProducer("world/america/canada")
            .ProduceAsync(new TestEventOne { ContentEventOne = "Canada" });
        await GetProducer("world/america/canada/news")
            .ProduceAsync(new TestEventOne { ContentEventOne = "Canada News" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        Helper.Spy.InboundEnvelopes.Select(envelope => envelope.Message.As<TestEventOne>().ContentEventOne)
            .Should().BeEquivalentTo("Mesocco", "Ticino", "USA News", "California News", "Canada News");
    }
}
