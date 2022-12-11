// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Text;
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
                .UseModel()
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

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "two" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "three" });

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(3);
        messages[0].Key.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("one"));
        messages[1].Key.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("two"));
        messages[2].Key.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("three"));
    }

    [Fact]
    public async Task WithKafkaKey_ShouldSetMessageKeyFromEnvelope()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
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

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "two" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "three" });

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(3);
        messages[0].Key.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("one"));
        messages[1].Key.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("two"));
        messages[2].Key.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("three"));
    }

    [Fact]
    public async Task WithKafka_ShouldSetMessageKeyByMessageType()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
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

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishAsync(new TestEventTwo { ContentEventTwo = "two" });
        await publisher.PublishAsync(new TestEventThree { ContentEventThree = "three" });

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(3);
        messages[0].Key.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("one"));
        messages[1].Key.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("two"));
        messages[2].Key.Should().NotBeEmpty();
        messages[2].Key.Should().NotBeEquivalentTo(Encoding.UTF8.GetBytes("three"));
    }

    [Fact]
    public async Task WithKafkaKey_ShouldSetMessageKeyToNull_WhenFunctionReturnsNull()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .UseModel()
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

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "two" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "three" });

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(3);
        messages[0].Key.Should().BeNull();
        messages[1].Key.Should().BeNull();
        messages[2].Key.Should().BeNull();
    }
}
