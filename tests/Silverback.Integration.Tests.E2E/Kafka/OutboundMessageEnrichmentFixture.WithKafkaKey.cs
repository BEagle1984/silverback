// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class OutboundMessageEnrichmentFixture
{
    [Fact]
    public async Task WithKafkaKey_ShouldSetMessageKeyFromMessage()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer.Produce<TestEventOne>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName)
                                    .WithKafkaKey(message => message?.ContentEventOne)))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "two" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "three" });

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(3);
        messages[0].Key.Should().BeEquivalentTo("one"u8.ToArray());
        messages[1].Key.Should().BeEquivalentTo("two"u8.ToArray());
        messages[2].Key.Should().BeEquivalentTo("three"u8.ToArray());
    }

    [Fact]
    public async Task WithKafkaKey_ShouldSetMessageKeyFromEnvelope()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer.Produce<TestEventOne>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName)
                                    .WithKafkaKey(envelope => envelope.Message?.ContentEventOne)))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "two" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "three" });

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(3);
        messages[0].Key.Should().BeEquivalentTo("one"u8.ToArray());
        messages[1].Key.Should().BeEquivalentTo("two"u8.ToArray());
        messages[2].Key.Should().BeEquivalentTo("three"u8.ToArray());
    }

    [Fact]
    public async Task WithKafkaKey_ShouldSetMessageKeyByMessageType()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer.Produce<IIntegrationEvent>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName)
                                    .WithKafkaKey<TestEventOne>(message => message?.ContentEventOne)
                                    .WithKafkaKey<TestEventTwo>(envelope => envelope.Message?.ContentEventTwo)))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = "two" });
        await publisher.PublishEventAsync(new TestEventThree { ContentEventThree = "three" });

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(3);
        messages[0].Key.Should().BeEquivalentTo("one"u8.ToArray());
        messages[1].Key.Should().BeEquivalentTo("two"u8.ToArray());
        messages[2].Key.Should().BeNull();
    }

    [Fact]
    public async Task WithKafkaKey_ShouldSetMessageKeyToNull_WhenFunctionReturnsNull()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(
                            producer => producer.Produce<IIntegrationEvent>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName)
                                    .WithKafkaKey((TestEventOne? _) => null)))));

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "two" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "three" });

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(3);
        messages[0].Key.Should().BeNull();
        messages[1].Key.Should().BeNull();
        messages[2].Key.Should().BeNull();
    }
}
