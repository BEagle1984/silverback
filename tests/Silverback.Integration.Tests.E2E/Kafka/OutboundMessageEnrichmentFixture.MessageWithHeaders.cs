// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EnrichedMessages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class OutboundMessageEnrichmentFixture
{
    [Fact]
    public async Task MessageWithHeaders_ShouldBeProduced()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer.Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .AddIntegrationSpy());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne().AddHeader("one", 1).AddHeader("two", 2));
        await publisher.PublishEventAsync(new TestEventTwo().AddHeader("three", 3).AddHeader("four", 4));

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.OutboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
        Helper.Spy.OutboundEnvelopes[0].Headers.Should().ContainSingle(header => header.Name == "one" && header.Value == "1");
        Helper.Spy.OutboundEnvelopes[0].Headers.Should().ContainSingle(header => header.Name == "two" && header.Value == "2");
        Helper.Spy.OutboundEnvelopes[1].Message.Should().BeOfType<TestEventTwo>();
        Helper.Spy.OutboundEnvelopes[1].Headers.Should().ContainSingle(header => header.Name == "three" && header.Value == "3");
        Helper.Spy.OutboundEnvelopes[1].Headers.Should().ContainSingle(header => header.Name == "four" && header.Value == "4");
    }
}
