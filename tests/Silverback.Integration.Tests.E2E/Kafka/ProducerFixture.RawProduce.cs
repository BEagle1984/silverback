// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.IO;
using System.Threading;
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

public partial class ProducerFixture
{
    [Fact]
    public async Task RawProduce_ShouldProduceByteArray()
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

        producer.RawProduce(
            [0x01, 0x01, 0x01, 0x01, 0x01],
            new MessageHeaderCollection { { "x-custom", "test 1" }, { "two", "2" } });
        producer.RawProduce(
            [0x02, 0x02, 0x02, 0x02, 0x02],
            new MessageHeaderCollection { { "x-custom", "test 2" }, { "two", "2" } });
        producer.RawProduce(
            [0x03, 0x03, 0x03, 0x03, 0x03],
            new MessageHeaderCollection { { "x-custom", "test 3" }, { "two", "2" } });

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
    public async Task RawProduce_ShouldProduceStream()
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

        producer.RawProduce(
            new MemoryStream([0x01, 0x01, 0x01, 0x01, 0x01]),
            new MessageHeaderCollection { { "x-custom", "test 1" }, { "two", "2" } });
        producer.RawProduce(
            new MemoryStream([0x02, 0x02, 0x02, 0x02, 0x02]),
            new MessageHeaderCollection { { "x-custom", "test 2" }, { "two", "2" } });
        producer.RawProduce(
            new MemoryStream([0x03, 0x03, 0x03, 0x03, 0x03]),
            new MessageHeaderCollection { { "x-custom", "test 3" }, { "two", "2" } });

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
    public async Task RawProduce_ShouldProduceByteArrayUsingCallbacks()
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

        KafkaProducer producer = (KafkaProducer)Helper.GetProducerForEndpoint(DefaultTopicName);

        producer.RawProduce(
            [0x01, 0x01, 0x01, 0x01, 0x01],
            new MessageHeaderCollection { { "x-custom", "test 1" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        producer.RawProduce(
            [0x02, 0x02, 0x02, 0x02, 0x02],
            new MessageHeaderCollection { { "x-custom", "test 2" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        producer.RawProduce(
            [0x03, 0x03, 0x03, 0x03, 0x03],
            new MessageHeaderCollection { { "x-custom", "test 3" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));

        produced.ShouldBeLessThan(3);

        await AsyncTestingUtil.WaitAsync(() => produced == 3);

        produced.ShouldBe(3);
        errors.ShouldBe(0);

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
    public async Task RawProduce_ShouldProduceStreamUsingCallbacks()
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

        KafkaProducer producer = (KafkaProducer)Helper.GetProducerForEndpoint(DefaultTopicName);

        producer.RawProduce(
            new MemoryStream([0x01, 0x01, 0x01, 0x01, 0x01]),
            new MessageHeaderCollection { { "x-custom", "test 1" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        producer.RawProduce(
            new MemoryStream([0x02, 0x02, 0x02, 0x02, 0x02]),
            new MessageHeaderCollection { { "x-custom", "test 2" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        producer.RawProduce(
            new MemoryStream([0x03, 0x03, 0x03, 0x03, 0x03]),
            new MessageHeaderCollection { { "x-custom", "test 3" }, { "two", "2" } },
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));

        produced.ShouldBeLessThan(3);

        await AsyncTestingUtil.WaitAsync(() => produced == 3);

        produced.ShouldBe(3);
        errors.ShouldBe(0);

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
    public async Task RawProduce_ShouldSetKafkaKeyFromMessageIdHeader()
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
        messages.Count.ShouldBe(3);
        messages[0].Key.ShouldBe("1001"u8.ToArray());
        messages[1].Key.ShouldBe("2002"u8.ToArray());
        messages[2].Key.ShouldBe("3003"u8.ToArray());
    }
}
