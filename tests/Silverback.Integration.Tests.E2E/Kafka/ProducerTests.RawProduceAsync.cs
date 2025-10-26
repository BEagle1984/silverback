// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ProducerTests
{
    [Fact]
    public async Task RawProduceAsync_ShouldProduceByteArray()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync([0x01, 0x01, 0x01, 0x01, 0x01]);
        await producer.RawProduceAsync([0x02, 0x02, 0x02, 0x02, 0x02]);
        await producer.RawProduceAsync([0x03, 0x03, 0x03, 0x03, 0x03]);

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].Value.ShouldBe([0x01, 0x01, 0x01, 0x01, 0x01]);
        messages[1].Value.ShouldBe([0x02, 0x02, 0x02, 0x02, 0x02]);
        messages[2].Value.ShouldBe([0x03, 0x03, 0x03, 0x03, 0x03]);
    }

    [Fact]
    public async Task RawProduceAsync_ShouldProduceByteArrayConfiguringEnvelope()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync(
            [0x01, 0x01, 0x01, 0x01, 0x01],
            envelope => envelope.AddHeader("x-custom", "test 1").AddHeader("two", "2"));
        await producer.RawProduceAsync(
            [0x02, 0x02, 0x02, 0x02, 0x02],
            envelope => envelope.AddHeader("x-custom", "test 2").AddHeader("two", "2"));
        await producer.RawProduceAsync(
            [0x03, 0x03, 0x03, 0x03, 0x03],
            envelope => envelope.AddHeader("x-custom", "test 3").AddHeader("two", "2"));

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].Value.ShouldBe([0x01, 0x01, 0x01, 0x01, 0x01]);
        messages[0].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 1");
        messages[1].Value.ShouldBe([0x02, 0x02, 0x02, 0x02, 0x02]);
        messages[1].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 2");
        messages[2].Value.ShouldBe([0x03, 0x03, 0x03, 0x03, 0x03]);
        messages[2].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 3");
    }

    [Fact]
    public async Task RawProduceAsync_ShouldProduceStream()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync(new MemoryStream([0x01, 0x01, 0x01, 0x01, 0x01]));
        await producer.RawProduceAsync(new MemoryStream([0x02, 0x02, 0x02, 0x02, 0x02]));
        await producer.RawProduceAsync(new MemoryStream([0x03, 0x03, 0x03, 0x03, 0x03]));

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].Value.ShouldBe([0x01, 0x01, 0x01, 0x01, 0x01]);
        messages[1].Value.ShouldBe([0x02, 0x02, 0x02, 0x02, 0x02]);
        messages[2].Value.ShouldBe([0x03, 0x03, 0x03, 0x03, 0x03]);
    }

    [Fact]
    public async Task RawProduceAsync_ShouldProduceStreamConfiguringEnvelope()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync(
            new MemoryStream([0x01, 0x01, 0x01, 0x01, 0x01]),
            envelope => envelope.AddHeader("x-custom", "test 1").AddHeader("two", "2"));
        await producer.RawProduceAsync(
            new MemoryStream([0x02, 0x02, 0x02, 0x02, 0x02]),
            envelope => envelope.AddHeader("x-custom", "test 2").AddHeader("two", "2"));
        await producer.RawProduceAsync(
            new MemoryStream([0x03, 0x03, 0x03, 0x03, 0x03]),
            envelope => envelope.AddHeader("x-custom", "test 3").AddHeader("two", "2"));

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].Value.ShouldBe([0x01, 0x01, 0x01, 0x01, 0x01]);
        messages[0].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 1");
        messages[1].Value.ShouldBe([0x02, 0x02, 0x02, 0x02, 0x02]);
        messages[1].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 2");
        messages[2].Value.ShouldBe([0x03, 0x03, 0x03, 0x03, 0x03]);
        messages[2].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 3");
    }

    [Fact]
    public async Task RawProduceAsync_ShouldProduceEnvelope()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync(
            producer.EnvelopeFactory.Create(new MemoryStream([0x01, 0x01, 0x01, 0x01, 0x01]))
                .AddHeader("x-custom", "test 1").AddHeader("two", "2"));
        await producer.RawProduceAsync(
            producer.EnvelopeFactory.Create(new MemoryStream([0x02, 0x02, 0x02, 0x02, 0x02]))
                .AddHeader("x-custom", "test 2").AddHeader("two", "2"));
        await producer.RawProduceAsync(
            producer.EnvelopeFactory.Create(new MemoryStream([0x03, 0x03, 0x03, 0x03, 0x03]))
                .AddHeader("x-custom", "test 3").AddHeader("two", "2"));

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].Value.ShouldBe([0x01, 0x01, 0x01, 0x01, 0x01]);
        messages[0].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 1");
        messages[1].Value.ShouldBe([0x02, 0x02, 0x02, 0x02, 0x02]);
        messages[1].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 2");
        messages[2].Value.ShouldBe([0x03, 0x03, 0x03, 0x03, 0x03]);
        messages[2].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 3");
    }

    [Fact]
    public async Task RawProduceAsync_ShouldSetKafkaKeyFromEnvelope()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.RawProduceAsync(
            BytesUtil.GetRandomBytes(),
            envelope => envelope.SetKafkaRawKey("1001"u8.ToArray()));
        await producer.RawProduceAsync(
            BytesUtil.GetRandomBytes(),
            envelope => envelope.SetKafkaRawKey("2002"u8.ToArray()));
        await producer.RawProduceAsync(
            BytesUtil.GetRandomBytes(),
            envelope => envelope.SetKafkaRawKey("3003"u8.ToArray()));

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].Key.ShouldBe("1001"u8.ToArray());
        messages[1].Key.ShouldBe("2002"u8.ToArray());
        messages[2].Key.ShouldBe("3003"u8.ToArray());
    }
}
