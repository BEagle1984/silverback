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

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ProducerEndpointFixture
{
    [Fact]
    public async Task ProducerEndpoint_ShouldProduceTombstone()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(producer => producer.Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(new Tombstone<TestEventOne>("42"));

        DefaultTopic.MessagesCount.Should().Be(1);
        DefaultTopic.GetAllMessages()[0].Value.Should().BeNull();
        DefaultTopic.GetAllMessages()[0].Key.Should().BeEquivalentTo("42"u8.ToArray());
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldProduceTombstone_WhenUsingDynamicEndpoint()
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
                            producer => producer.Produce<IIntegrationEvent>(
                                endpoint => endpoint
                                    .ProduceTo(_ => DefaultTopicName)))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(new Tombstone<TestEventOne>("42"));

        DefaultTopic.MessagesCount.Should().Be(1);
        DefaultTopic.GetAllMessages()[0].Value.Should().BeNull();
        DefaultTopic.GetAllMessages()[0].Key.Should().BeEquivalentTo("42"u8.ToArray());
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldProduceTombstoneFromNull()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(producer => producer.Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.WrapAndPublishAsync<TestEventOne>(null, envelope => envelope.SetKafkaKey("42"));

        DefaultTopic.MessagesCount.Should().Be(1);
        DefaultTopic.GetAllMessages()[0].Value.Should().BeNull();
        DefaultTopic.GetAllMessages()[0].Key.Should().BeEquivalentTo("42"u8.ToArray());
    }
}
