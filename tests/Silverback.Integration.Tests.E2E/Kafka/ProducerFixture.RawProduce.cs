// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ProducerFixture
{
    [Fact]
    public async Task RawProduce_ShouldProduceByteArray()
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
                            producer => producer
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        producer.RawProduce(
            new byte[] { 0x01, 0x01, 0x01, 0x01, 0x01 },
            new MessageHeaderCollection { { "x-custom", "test 1" }, { "two", "2" } });
        producer.RawProduce(
            new byte[] { 0x02, 0x02, 0x02, 0x02, 0x02 },
            new MessageHeaderCollection { { "x-custom", "test 2" }, { "two", "2" } });
        producer.RawProduce(
            new byte[] { 0x03, 0x03, 0x03, 0x03, 0x03 },
            new MessageHeaderCollection { { "x-custom", "test 3" }, { "two", "2" } });

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(3);
        messages[0].Value.Should().BeEquivalentTo(new byte[] { 0x01, 0x01, 0x01, 0x01, 0x01 });
        messages[0].Headers.Select(header => (header.Key, header.GetValueAsString())).Should().ContainEquivalentOf(("x-custom", "test 1"));
        messages[1].Value.Should().BeEquivalentTo(new byte[] { 0x02, 0x02, 0x02, 0x02, 0x02 });
        messages[1].Headers.Select(header => (header.Key, header.GetValueAsString())).Should().ContainEquivalentOf(("x-custom", "test 2"));
        messages[2].Value.Should().BeEquivalentTo(new byte[] { 0x03, 0x03, 0x03, 0x03, 0x03 });
        messages[2].Headers.Select(header => (header.Key, header.GetValueAsString())).Should().ContainEquivalentOf(("x-custom", "test 3"));
    }

    [Fact]
    public async Task RawProduce_ShouldProduceStream()
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
                            producer => producer
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        producer.RawProduce(
            new MemoryStream(new byte[] { 0x01, 0x01, 0x01, 0x01, 0x01 }),
            new MessageHeaderCollection { { "x-custom", "test 1" }, { "two", "2" } });
        producer.RawProduce(
            new MemoryStream(new byte[] { 0x02, 0x02, 0x02, 0x02, 0x02 }),
            new MessageHeaderCollection { { "x-custom", "test 2" }, { "two", "2" } });
        producer.RawProduce(
            new MemoryStream(new byte[] { 0x03, 0x03, 0x03, 0x03, 0x03 }),
            new MessageHeaderCollection { { "x-custom", "test 3" }, { "two", "2" } });

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(3);
        messages[0].Value.Should().BeEquivalentTo(new byte[] { 0x01, 0x01, 0x01, 0x01, 0x01 });
        messages[0].Headers.Select(header => (header.Key, header.GetValueAsString())).Should().ContainEquivalentOf(("x-custom", "test 1"));
        messages[1].Value.Should().BeEquivalentTo(new byte[] { 0x02, 0x02, 0x02, 0x02, 0x02 });
        messages[1].Headers.Select(header => (header.Key, header.GetValueAsString())).Should().ContainEquivalentOf(("x-custom", "test 2"));
        messages[2].Value.Should().BeEquivalentTo(new byte[] { 0x03, 0x03, 0x03, 0x03, 0x03 });
        messages[2].Headers.Select(header => (header.Key, header.GetValueAsString())).Should().ContainEquivalentOf(("x-custom", "test 3"));
    }

    [Fact]
    public async Task RawProduce_ShouldProduceByteArrayUsingCallbacks()
    {
        int produced = 0;
        int errors = 0;

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
                            producer => producer
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        KafkaProducer producer = (KafkaProducer)Helper.GetProducerForEndpoint(DefaultTopicName);

        producer.RawProduce(
            new byte[] { 0x01, 0x01, 0x01, 0x01, 0x01 },
            new MessageHeaderCollection { { "x-custom", "test 1" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        producer.RawProduce(
            new byte[] { 0x02, 0x02, 0x02, 0x02, 0x02 },
            new MessageHeaderCollection { { "x-custom", "test 2" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        producer.RawProduce(
            new byte[] { 0x03, 0x03, 0x03, 0x03, 0x03 },
            new MessageHeaderCollection { { "x-custom", "test 3" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));

        produced.Should().BeLessThan(3);

        await AsyncTestingUtil.WaitAsync(() => produced == 3);

        produced.Should().Be(3);
        errors.Should().Be(0);

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(3);
        messages[0].Value.Should().BeEquivalentTo(new byte[] { 0x01, 0x01, 0x01, 0x01, 0x01 });
        messages[0].Headers.Select(header => (header.Key, header.GetValueAsString())).Should().ContainEquivalentOf(("x-custom", "test 1"));
        messages[1].Value.Should().BeEquivalentTo(new byte[] { 0x02, 0x02, 0x02, 0x02, 0x02 });
        messages[1].Headers.Select(header => (header.Key, header.GetValueAsString())).Should().ContainEquivalentOf(("x-custom", "test 2"));
        messages[2].Value.Should().BeEquivalentTo(new byte[] { 0x03, 0x03, 0x03, 0x03, 0x03 });
        messages[2].Headers.Select(header => (header.Key, header.GetValueAsString())).Should().ContainEquivalentOf(("x-custom", "test 3"));
    }

    [Fact]
    public async Task RawProduce_ShouldProduceStreamUsingCallbacks()
    {
        int produced = 0;
        int errors = 0;

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
                            producer => producer
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        KafkaProducer producer = (KafkaProducer)Helper.GetProducerForEndpoint(DefaultTopicName);

        producer.RawProduce(
            new MemoryStream(new byte[] { 0x01, 0x01, 0x01, 0x01, 0x01 }),
            new MessageHeaderCollection { { "x-custom", "test 1" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        producer.RawProduce(
            new MemoryStream(new byte[] { 0x02, 0x02, 0x02, 0x02, 0x02 }),
            new MessageHeaderCollection { { "x-custom", "test 2" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        producer.RawProduce(
            new MemoryStream(new byte[] { 0x03, 0x03, 0x03, 0x03, 0x03 }),
            new MessageHeaderCollection { { "x-custom", "test 3" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));

        produced.Should().BeLessThan(3);

        await AsyncTestingUtil.WaitAsync(() => produced == 3);

        produced.Should().Be(3);
        errors.Should().Be(0);

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(3);
        messages[0].Value.Should().BeEquivalentTo(new byte[] { 0x01, 0x01, 0x01, 0x01, 0x01 });
        messages[0].Headers.Select(header => (header.Key, header.GetValueAsString())).Should().ContainEquivalentOf(("x-custom", "test 1"));
        messages[1].Value.Should().BeEquivalentTo(new byte[] { 0x02, 0x02, 0x02, 0x02, 0x02 });
        messages[1].Headers.Select(header => (header.Key, header.GetValueAsString())).Should().ContainEquivalentOf(("x-custom", "test 2"));
        messages[2].Value.Should().BeEquivalentTo(new byte[] { 0x03, 0x03, 0x03, 0x03, 0x03 });
        messages[2].Headers.Select(header => (header.Key, header.GetValueAsString())).Should().ContainEquivalentOf(("x-custom", "test 3"));
    }

    [Fact]
    public async Task RawProduce_ShouldSetKafkaKeyFromMessageIdHeader()
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
                            producer => producer
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        producer.RawProduce(
            BytesUtil.GetRandomBytes(),
            new MessageHeaderCollection { { DefaultMessageHeaders.MessageId, "1001" } });
        producer.RawProduce(
            BytesUtil.GetRandomBytes(),
            new MessageHeaderCollection { { DefaultMessageHeaders.MessageId, "2002" } });
        producer.RawProduce(
            BytesUtil.GetRandomBytes(),
            new MessageHeaderCollection { { DefaultMessageHeaders.MessageId, "3003" } });

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(3);
        messages[0].Key.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("1001"));
        messages[1].Key.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("2002"));
        messages[2].Key.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("3003"));
    }

    [Fact]
    public async Task RawProduce_ShouldSetKafkaKeyFromKafkaKeyHeader()
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
                            producer => producer
                                .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        producer.RawProduce(
            BytesUtil.GetRandomBytes(),
            new MessageHeaderCollection { { KafkaMessageHeaders.KafkaMessageKey, "1001" } });
        producer.RawProduce(
            BytesUtil.GetRandomBytes(),
            new MessageHeaderCollection { { KafkaMessageHeaders.KafkaMessageKey, "2002" } });
        producer.RawProduce(
            BytesUtil.GetRandomBytes(),
            new MessageHeaderCollection { { KafkaMessageHeaders.KafkaMessageKey, "3003" } });

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(3);
        messages[0].Key.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("1001"));
        messages[1].Key.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("2002"));
        messages[2].Key.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("3003"));
    }
}
