// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.Routing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ProducerFixture
{
    [Fact]
    public async Task ProduceAsync_ShouldProduceMessage()
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
                            producer => producer
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        DefaultTopic.MessagesCount.Should().Be(3);
        DefaultTopic.GetAllMessages().GetContentAsString().Should().BeEquivalentTo(
            [
                "{\"ContentEventOne\":\"1\"}",
                "{\"ContentEventOne\":\"2\"}",
                "{\"ContentEventOne\":\"3\"}"
            ],
            options => options.WithoutStrictOrdering());
    }

    [Fact]
    public async Task ProduceAsync_ShouldProduceMessageWithHeaders()
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
                            producer => producer
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(
                new TestEventOne { ContentEventOne = $"Hello E2E {i}!" },
                new MessageHeaderCollection { { "x-custom", $"test {i}" }, { "two", "2" } });
        }

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(3);
        messages[0].GetContentAsString().Should().BeEquivalentTo("{\"ContentEventOne\":\"Hello E2E 1!\"}");
        messages[0].Headers.Select(header => (header.Key, header.GetValueAsString())).Should().ContainEquivalentOf(("x-custom", "test 1"));
        messages[1].GetContentAsString().Should().BeEquivalentTo("{\"ContentEventOne\":\"Hello E2E 2!\"}");
        messages[1].Headers.Select(header => (header.Key, header.GetValueAsString())).Should().ContainEquivalentOf(("x-custom", "test 2"));
        messages[2].GetContentAsString().Should().BeEquivalentTo("{\"ContentEventOne\":\"Hello E2E 3!\"}");
        messages[2].Headers.Select(header => (header.Key, header.GetValueAsString())).Should().ContainEquivalentOf(("x-custom", "test 3"));
    }

    [Fact]
    public async Task ProduceAsync_ShouldProduceEnvelope()
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
                            producer => producer
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IOutboundEnvelopeFactory envelopeFactory = Host.ServiceProvider.GetRequiredService<IOutboundEnvelopeFactory>();
        KafkaProducer producer = (KafkaProducer)Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            await producer.ProduceAsync(
                envelopeFactory.CreateEnvelope(
                    new TestEventOne { ContentEventOne = $"Hello E2E {i}!" },
                    new MessageHeaderCollection { { "x-custom", $"test {i}" }, { "two", "2" } },
                    new KafkaProducerEndpoint(DefaultTopicName, Partition.Any, producer.EndpointConfiguration),
                    producer));
        }

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(3);
        messages[0].GetContentAsString().Should().BeEquivalentTo("{\"ContentEventOne\":\"Hello E2E 1!\"}");
        messages[0].Headers.Select(header => (header.Key, header.GetValueAsString())).Should().ContainEquivalentOf(("x-custom", "test 1"));
        messages[1].GetContentAsString().Should().BeEquivalentTo("{\"ContentEventOne\":\"Hello E2E 2!\"}");
        messages[1].Headers.Select(header => (header.Key, header.GetValueAsString())).Should().ContainEquivalentOf(("x-custom", "test 2"));
        messages[2].GetContentAsString().Should().BeEquivalentTo("{\"ContentEventOne\":\"Hello E2E 3!\"}");
        messages[2].Headers.Select(header => (header.Key, header.GetValueAsString())).Should().ContainEquivalentOf(("x-custom", "test 3"));
    }

    [Fact]
    public async Task ProduceAsync_ShouldProduceMessageUsingCallbacks()
    {
        int produced = 0;
        int errors = 0;

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
                            producer => producer
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            producer.Produce(
                new TestEventOne { ContentEventOne = $"Hello E2E {i}!" },
                new MessageHeaderCollection { { "x-custom", $"test {i}" } },
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));
        }

        produced.Should().BeLessThan(3);

        await AsyncTestingUtil.WaitAsync(() => produced == 3);

        produced.Should().Be(3);
        errors.Should().Be(0);

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(3);
        messages[0].GetContentAsString().Should().BeEquivalentTo("{\"ContentEventOne\":\"Hello E2E 1!\"}");
        messages[0].Headers.Select(header => (header.Key, header.GetValueAsString())).Should().ContainEquivalentOf(("x-custom", "test 1"));
        messages[1].GetContentAsString().Should().BeEquivalentTo("{\"ContentEventOne\":\"Hello E2E 2!\"}");
        messages[1].Headers.Select(header => (header.Key, header.GetValueAsString())).Should().ContainEquivalentOf(("x-custom", "test 2"));
        messages[2].GetContentAsString().Should().BeEquivalentTo("{\"ContentEventOne\":\"Hello E2E 3!\"}");
        messages[2].Headers.Select(header => (header.Key, header.GetValueAsString())).Should().ContainEquivalentOf(("x-custom", "test 3"));
    }

    [Fact]
    public async Task ProduceAsync_ShouldSetKafkaKeyFromMessageIdHeader()
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
                            producer => producer
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(
            new TestEventOne(),
            new MessageHeaderCollection { { DefaultMessageHeaders.MessageId, "1001" } });
        await producer.ProduceAsync(
            new TestEventOne(),
            new MessageHeaderCollection { { DefaultMessageHeaders.MessageId, "2002" } });
        await producer.ProduceAsync(
            new TestEventOne(),
            new MessageHeaderCollection { { DefaultMessageHeaders.MessageId, "3003" } });

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(3);
        messages[0].Key.Should().BeEquivalentTo("1001"u8.ToArray());
        messages[1].Key.Should().BeEquivalentTo("2002"u8.ToArray());
        messages[2].Key.Should().BeEquivalentTo("3003"u8.ToArray());
    }

    [Fact]
    public async Task ProduceAsync_ShouldSetKafkaKeyFromTombstone()
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
                            producer => producer
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new Tombstone<TestEventOne>("1001"));
        await producer.ProduceAsync(new Tombstone<TestEventOne>("2002"));
        await producer.ProduceAsync(new Tombstone<TestEventOne>("3003"));

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(3);
        messages[0].Key.Should().BeEquivalentTo("1001"u8.ToArray());
        messages[1].Key.Should().BeEquivalentTo("2002"u8.ToArray());
        messages[2].Key.Should().BeEquivalentTo("3003"u8.ToArray());
    }
}
