// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
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
    public async Task ProducerEndpoint_ShouldProduceBatch()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(producer => producer.Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(new[] { new TestEventOne(), new TestEventOne() });
        await publisher.PublishAsync(new IIntegrationEvent[] { new TestEventTwo(), new TestEventOne() });

        DefaultTopic.MessagesCount.Should().Be(4);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldProduceAsyncBatch()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(producer => producer.Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))));

        static async IAsyncEnumerable<IIntegrationEvent> GetMessagesAsync()
        {
            yield return new TestEventOne();
            await Task.Delay(10);
            yield return new TestEventTwo();
            await Task.Delay(20);
            yield return new TestEventThree();
        }

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishAsync(GetMessagesAsync());

        DefaultTopic.MessagesCount.Should().Be(3);
    }
}
