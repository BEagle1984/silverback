// Copyright (c) 2023 Sergio Aquilini
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
    public async Task WithMessageId_ShouldSetMessageKeyFromMessage()
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
                                    .WithMessageId(message => message?.ContentEventOne)))));

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "two" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "three" });

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(3);
        messages[0].Key.Should().BeEquivalentTo("one"u8.ToArray());
        messages[1].Key.Should().BeEquivalentTo("two"u8.ToArray());
        messages[2].Key.Should().BeEquivalentTo("three"u8.ToArray());
    }

    [Fact]
    public async Task WithMessageId_ShouldSetMessageKeyFromEnvelope()
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
                                    .WithMessageId(envelope => envelope.Message?.ContentEventOne)))));

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "two" });
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "three" });

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(3);
        messages[0].Key.Should().BeEquivalentTo("one"u8.ToArray());
        messages[1].Key.Should().BeEquivalentTo("two"u8.ToArray());
        messages[2].Key.Should().BeEquivalentTo("three"u8.ToArray());
    }

    [Fact]
    public async Task WithMessageId_ShouldSetMessageKeyByMessageType()
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
                                    .WithMessageId<TestEventOne>(message => message?.ContentEventOne)
                                    .WithMessageId<TestEventTwo>(envelope => envelope.Message?.ContentEventTwo)))));

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishAsync(new TestEventTwo { ContentEventTwo = "two" });
        await publisher.PublishAsync(new TestEventThree { ContentEventThree = "three" });

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(3);
        messages[0].Key.Should().BeEquivalentTo("one"u8.ToArray());
        messages[1].Key.Should().BeEquivalentTo("two"u8.ToArray());
        messages[2].Key.Should().BeNull();
    }

    [Fact]
    public async Task WithMessageId_ShouldSetMessageKeyToNull_WhenFunctionReturnsNull()
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
                                    .WithMessageId((TestEventOne? _) => null)))));

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
