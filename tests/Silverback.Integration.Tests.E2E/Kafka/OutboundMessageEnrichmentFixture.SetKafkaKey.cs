// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
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
    public async Task SetKafkaKey_ShouldSetMessageKeyFromMessage()
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
                                    .SetKafkaKey(message => message?.ContentEventOne)))));

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "two" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "three" });

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].Key.ShouldBe("one"u8.ToArray());
        messages[1].Key.ShouldBe("two"u8.ToArray());
        messages[2].Key.ShouldBe("three"u8.ToArray());
    }

    [Fact]
    public async Task SetKafkaKey_ShouldSetMessageKeyFromEnvelope()
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
                                    .SetKafkaKey(envelope => envelope.Message?.ContentEventOne)))));

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "two" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "three" });

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].Key.ShouldBe("one"u8.ToArray());
        messages[1].Key.ShouldBe("two"u8.ToArray());
        messages[2].Key.ShouldBe("three"u8.ToArray());
    }

    [Fact]
    public async Task SetKafkaKey_ShouldSetMessageKeyByMessageType()
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
                                    .SetKafkaKey<TestEventOne>(message => message?.ContentEventOne)
                                    .SetKafkaKey<TestEventTwo>(envelope => envelope.Message?.ContentEventTwo)))));

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = "two" });
        await publisher.PublishEventAsync(new TestEventThree { ContentEventThree = "three" });

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].Key.ShouldBe("one"u8.ToArray());
        messages[1].Key.ShouldBe("two"u8.ToArray());
        messages[2].Key.ShouldBeNull();
    }

    [Fact]
    public async Task SetKafkaKey_ShouldSetMessageKeyToNull_WhenFunctionReturnsNull()
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
                                    .SetKafkaKey((TestEventOne? _) => null)))));

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "two" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "three" });

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].Key.ShouldBeNull();
        messages[1].Key.ShouldBeNull();
        messages[2].Key.ShouldBeNull();
    }
}
