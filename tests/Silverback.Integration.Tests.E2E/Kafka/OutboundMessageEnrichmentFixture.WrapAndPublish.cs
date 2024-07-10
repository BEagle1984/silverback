// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
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
    public async Task WrapAndPublishAsync_ShouldProduceEnrichedEnvelopes()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka(kafkaOptions => kafkaOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(producer => producer.Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .AddIntegrationSpy());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        int i = 0;
        await publisher.WrapAndPublishAsync(
            new TestEventOne(),
            envelope => envelope.AddHeader("one", "1").AddHeader("two", "2").SetKafkaKey($"{++i}"));
        await publisher.WrapAndPublishAsync(
            new TestEventTwo(),
            envelope => envelope.AddHeader("three", "3").AddHeader("four", "4").SetKafkaKey($"{++i}"));

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.OutboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
        Helper.Spy.OutboundEnvelopes[0].Headers.Should().ContainSingle(header => header.Name == "one" && header.Value == "1");
        Helper.Spy.OutboundEnvelopes[0].Headers.Should().ContainSingle(header => header.Name == "two" && header.Value == "2");
        Helper.Spy.OutboundEnvelopes[1].Message.Should().BeOfType<TestEventTwo>();
        Helper.Spy.OutboundEnvelopes[1].Headers.Should().ContainSingle(header => header.Name == "three" && header.Value == "3");
        Helper.Spy.OutboundEnvelopes[1].Headers.Should().ContainSingle(header => header.Name == "four" && header.Value == "4");

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(2);
        messages[0].Key.Should().BeEquivalentTo("1"u8.ToArray());
        messages[1].Key.Should().BeEquivalentTo("2"u8.ToArray());
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldProduceEnrichedEnvelopes()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka(kafkaOptions => kafkaOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(producer => producer.Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .AddIntegrationSpy());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        int i = 0;
        await publisher.WrapAndPublishBatchAsync(
            [new TestEventOne(), new TestEventOne()],
            envelope => envelope.AddHeader("one", "1").AddHeader("two", "2").SetKafkaKey($"{++i}"));
        await publisher.WrapAndPublishBatchAsync(
            new IIntegrationEvent[] { new TestEventOne(), new TestEventTwo() }.AsEnumerable(),
            envelope => envelope.AddHeader("three", "3").AddHeader("four", "4").SetKafkaKey($"{++i + 10}"));

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(4);
        Helper.Spy.OutboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
        Helper.Spy.OutboundEnvelopes[0].Headers.Should().ContainSingle(header => header.Name == "one" && header.Value == "1");
        Helper.Spy.OutboundEnvelopes[0].Headers.Should().ContainSingle(header => header.Name == "two" && header.Value == "2");
        Helper.Spy.OutboundEnvelopes[1].Message.Should().BeOfType<TestEventOne>();
        Helper.Spy.OutboundEnvelopes[1].Headers.Should().ContainSingle(header => header.Name == "one" && header.Value == "1");
        Helper.Spy.OutboundEnvelopes[1].Headers.Should().ContainSingle(header => header.Name == "two" && header.Value == "2");
        Helper.Spy.OutboundEnvelopes[2].Message.Should().BeOfType<TestEventOne>();
        Helper.Spy.OutboundEnvelopes[2].Headers.Should().ContainSingle(header => header.Name == "three" && header.Value == "3");
        Helper.Spy.OutboundEnvelopes[2].Headers.Should().ContainSingle(header => header.Name == "four" && header.Value == "4");
        Helper.Spy.OutboundEnvelopes[3].Message.Should().BeOfType<TestEventTwo>();
        Helper.Spy.OutboundEnvelopes[3].Headers.Should().ContainSingle(header => header.Name == "three" && header.Value == "3");
        Helper.Spy.OutboundEnvelopes[3].Headers.Should().ContainSingle(header => header.Name == "four" && header.Value == "4");

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(4);
        messages[0].Key.Should().BeEquivalentTo("1"u8.ToArray());
        messages[1].Key.Should().BeEquivalentTo("2"u8.ToArray());
        messages[2].Key.Should().BeEquivalentTo("13"u8.ToArray());
        messages[3].Key.Should().BeEquivalentTo("14"u8.ToArray());
    }

    [Fact]
    public async Task WrapAndPublishBatchAsync_ShouldProduceEnrichedEnvelopesFromAsyncBatch()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka(kafkaOptions => kafkaOptions.WithDefaultPartitionsCount(1)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(producer => producer.Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .AddIntegrationSpy());

        IPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IPublisher>();
        await publisher.WrapAndPublishBatchAsync(
            new TestEventOne[] { new(), new() }.ToAsyncEnumerable(),
            static (envelope, counter) => envelope.AddHeader("one", "1").AddHeader("two", "2").SetKafkaKey($"{counter.Increment()}"),
            new Counter());
        await publisher.WrapAndPublishBatchAsync(
            new IIntegrationEvent[] { new TestEventOne(), new TestEventTwo() }.ToAsyncEnumerable(),
            (envelope, counter) => envelope.AddHeader("three", "3").AddHeader("four", "4").SetKafkaKey($"{counter.Increment()}"),
            new Counter(100));

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(4);
        Helper.Spy.OutboundEnvelopes[0].Message.Should().BeOfType<TestEventOne>();
        Helper.Spy.OutboundEnvelopes[0].Headers.Should().ContainSingle(header => header.Name == "one" && header.Value == "1");
        Helper.Spy.OutboundEnvelopes[0].Headers.Should().ContainSingle(header => header.Name == "two" && header.Value == "2");
        Helper.Spy.OutboundEnvelopes[1].Message.Should().BeOfType<TestEventOne>();
        Helper.Spy.OutboundEnvelopes[1].Headers.Should().ContainSingle(header => header.Name == "one" && header.Value == "1");
        Helper.Spy.OutboundEnvelopes[1].Headers.Should().ContainSingle(header => header.Name == "two" && header.Value == "2");
        Helper.Spy.OutboundEnvelopes[2].Message.Should().BeOfType<TestEventOne>();
        Helper.Spy.OutboundEnvelopes[2].Headers.Should().ContainSingle(header => header.Name == "three" && header.Value == "3");
        Helper.Spy.OutboundEnvelopes[2].Headers.Should().ContainSingle(header => header.Name == "four" && header.Value == "4");
        Helper.Spy.OutboundEnvelopes[3].Message.Should().BeOfType<TestEventTwo>();
        Helper.Spy.OutboundEnvelopes[3].Headers.Should().ContainSingle(header => header.Name == "three" && header.Value == "3");
        Helper.Spy.OutboundEnvelopes[3].Headers.Should().ContainSingle(header => header.Name == "four" && header.Value == "4");

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(4);
        messages[0].Key.Should().BeEquivalentTo("1"u8.ToArray());
        messages[1].Key.Should().BeEquivalentTo("2"u8.ToArray());
        messages[2].Key.Should().BeEquivalentTo("101"u8.ToArray());
        messages[3].Key.Should().BeEquivalentTo("102"u8.ToArray());
    }
}
