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

public partial class ProducerTests
{
    [Fact]
    public async Task RawProduce_ShouldProduceByteArray()
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

        producer.RawProduce([0x01, 0x01, 0x01, 0x01, 0x01]);
        producer.RawProduce([0x02, 0x02, 0x02, 0x02, 0x02]);
        producer.RawProduce([0x03, 0x03, 0x03, 0x03, 0x03]);

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].Value.ShouldBe([0x01, 0x01, 0x01, 0x01, 0x01]);
        messages[1].Value.ShouldBe([0x02, 0x02, 0x02, 0x02, 0x02]);
        messages[2].Value.ShouldBe([0x03, 0x03, 0x03, 0x03, 0x03]);
    }

    [Fact]
    public async Task RawProduce_ShouldProduceByteArrayConfiguringEnvelope()
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

        producer.RawProduce(
            [0x01, 0x01, 0x01, 0x01, 0x01],
            envelope => envelope.AddHeader("x-custom", "test 1").AddHeader("two", "2"));
        producer.RawProduce(
            [0x02, 0x02, 0x02, 0x02, 0x02],
            envelope => envelope.AddHeader("x-custom", "test 2").AddHeader("two", "2"));
        producer.RawProduce(
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
    public async Task RawProduce_ShouldProduceStream()
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

        producer.RawProduce(new MemoryStream([0x01, 0x01, 0x01, 0x01, 0x01]));
        producer.RawProduce(new MemoryStream([0x02, 0x02, 0x02, 0x02, 0x02]));
        producer.RawProduce(new MemoryStream([0x03, 0x03, 0x03, 0x03, 0x03]));

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].Value.ShouldBe([0x01, 0x01, 0x01, 0x01, 0x01]);
        messages[1].Value.ShouldBe([0x02, 0x02, 0x02, 0x02, 0x02]);
        messages[2].Value.ShouldBe([0x03, 0x03, 0x03, 0x03, 0x03]);
    }

    [Fact]
    public async Task RawProduce_ShouldProduceStreamConfiguringEnvelope()
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

        producer.RawProduce(
            new MemoryStream([0x01, 0x01, 0x01, 0x01, 0x01]),
            envelope => envelope.AddHeader("x-custom", "test 1").AddHeader("two", "2"));
        producer.RawProduce(
            new MemoryStream([0x02, 0x02, 0x02, 0x02, 0x02]),
            envelope => envelope.AddHeader("x-custom", "test 2").AddHeader("two", "2"));
        producer.RawProduce(
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
    public async Task RawProduce_ShouldProduceEnvelope()
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

        producer.RawProduce(
            producer.EnvelopeFactory.Create(new MemoryStream([0x01, 0x01, 0x01, 0x01, 0x01]))
                .AddHeader("x-custom", "test 1").AddHeader("two", "2"));
        producer.RawProduce(
            producer.EnvelopeFactory.Create(new MemoryStream([0x02, 0x02, 0x02, 0x02, 0x02]))
                .AddHeader("x-custom", "test 2").AddHeader("two", "2"));
        producer.RawProduce(
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
    public async Task RawProduce_ShouldProduceByteArrayUsingCallbacks()
    {
        int produced = 0;
        int errors = 0;

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

        KafkaProducer producer = (KafkaProducer)Helper.GetProducerForEndpoint(DefaultTopicName);

        producer.RawProduce(
            [0x01, 0x01, 0x01, 0x01, 0x01],
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        producer.RawProduce(
            [0x02, 0x02, 0x02, 0x02, 0x02],
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        producer.RawProduce(
            [0x03, 0x03, 0x03, 0x03, 0x03],
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));

        produced.ShouldBeLessThan(3);

        await AsyncTestingUtil.WaitAsync(() => produced == 3);

        produced.ShouldBe(3);
        errors.ShouldBe(0);

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].Value.ShouldBe([0x01, 0x01, 0x01, 0x01, 0x01]);
        messages[1].Value.ShouldBe([0x02, 0x02, 0x02, 0x02, 0x02]);
        messages[2].Value.ShouldBe([0x03, 0x03, 0x03, 0x03, 0x03]);
    }

    [Fact]
    public async Task RawProduce_ShouldProduceByteArrayUsingCallbacksConfiguringEnvelope()
    {
        int produced = 0;
        int errors = 0;

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

        KafkaProducer producer = (KafkaProducer)Helper.GetProducerForEndpoint(DefaultTopicName);

        producer.RawProduce(
            [0x01, 0x01, 0x01, 0x01, 0x01],
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors),
            envelope => envelope.AddHeader("x-custom", "test 1").AddHeader("two", "2"));
        producer.RawProduce(
            [0x02, 0x02, 0x02, 0x02, 0x02],
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors),
            envelope => envelope.AddHeader("x-custom", "test 2").AddHeader("two", "2"));
        producer.RawProduce(
            [0x03, 0x03, 0x03, 0x03, 0x03],
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors),
            envelope => envelope.AddHeader("x-custom", "test 3").AddHeader("two", "2"));

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

        KafkaProducer producer = (KafkaProducer)Helper.GetProducerForEndpoint(DefaultTopicName);

        producer.RawProduce(
            new MemoryStream([0x01, 0x01, 0x01, 0x01, 0x01]),
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        producer.RawProduce(
            new MemoryStream([0x02, 0x02, 0x02, 0x02, 0x02]),
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        producer.RawProduce(
            new MemoryStream([0x03, 0x03, 0x03, 0x03, 0x03]),
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));

        produced.ShouldBeLessThan(3);

        await AsyncTestingUtil.WaitAsync(() => produced == 3);

        produced.ShouldBe(3);
        errors.ShouldBe(0);

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].Value.ShouldBe([0x01, 0x01, 0x01, 0x01, 0x01]);
        messages[1].Value.ShouldBe([0x02, 0x02, 0x02, 0x02, 0x02]);
        messages[2].Value.ShouldBe([0x03, 0x03, 0x03, 0x03, 0x03]);
    }

    [Fact]
    public async Task RawProduce_ShouldProduceStreamUsingCallbacksConfiguringEnvelope()
    {
        int produced = 0;
        int errors = 0;

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

        KafkaProducer producer = (KafkaProducer)Helper.GetProducerForEndpoint(DefaultTopicName);

        producer.RawProduce(
            new MemoryStream([0x01, 0x01, 0x01, 0x01, 0x01]),
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors),
            envelope => envelope.AddHeader("x-custom", "test 1").AddHeader("two", "2"));
        producer.RawProduce(
            new MemoryStream([0x02, 0x02, 0x02, 0x02, 0x02]),
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors),
            envelope => envelope.AddHeader("x-custom", "test 2").AddHeader("two", "2"));
        producer.RawProduce(
            new MemoryStream([0x03, 0x03, 0x03, 0x03, 0x03]),
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors),
            envelope => envelope.AddHeader("x-custom", "test 3").AddHeader("two", "2"));

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
    public async Task RawProduce_ShouldProduceByteArrayUsingCallbacksWithState()
    {
        int produced = 0;
        int errors = 0;

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

        KafkaProducer producer = (KafkaProducer)Helper.GetProducerForEndpoint(DefaultTopicName);

        producer.RawProduce(
            [0x01, 0x01, 0x01, 0x01, 0x01],
            (_, increment) => Interlocked.Add(ref produced, increment),
            (_, increment) => Interlocked.Add(ref errors, increment),
            1);
        producer.RawProduce(
            [0x02, 0x02, 0x02, 0x02, 0x02],
            (_, increment) => Interlocked.Add(ref produced, increment),
            (_, increment) => Interlocked.Add(ref errors, increment),
            1);
        producer.RawProduce(
            [0x03, 0x03, 0x03, 0x03, 0x03],
            (_, increment) => Interlocked.Add(ref produced, increment),
            (_, increment) => Interlocked.Add(ref errors, increment),
            1);

        produced.ShouldBeLessThan(3);

        await AsyncTestingUtil.WaitAsync(() => produced == 3);

        produced.ShouldBe(3);
        errors.ShouldBe(0);

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].Value.ShouldBe([0x01, 0x01, 0x01, 0x01, 0x01]);
        messages[1].Value.ShouldBe([0x02, 0x02, 0x02, 0x02, 0x02]);
        messages[2].Value.ShouldBe([0x03, 0x03, 0x03, 0x03, 0x03]);
    }

    [Fact]
    public async Task RawProduce_ShouldProduceByteArrayUsingCallbacksWithStateConfiguringEnvelope()
    {
        int produced = 0;
        int errors = 0;

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

        KafkaProducer producer = (KafkaProducer)Helper.GetProducerForEndpoint(DefaultTopicName);

        producer.RawProduce(
            [0x01, 0x01, 0x01, 0x01, 0x01],
            (_, increment) => Interlocked.Add(ref produced, increment),
            (_, increment) => Interlocked.Add(ref errors, increment),
            1,
            envelope => envelope.AddHeader("x-custom", "test 1").AddHeader("two", "2"));
        producer.RawProduce(
            [0x02, 0x02, 0x02, 0x02, 0x02],
            (_, increment) => Interlocked.Add(ref produced, increment),
            (_, increment) => Interlocked.Add(ref errors, increment),
            1,
            envelope => envelope.AddHeader("x-custom", "test 2").AddHeader("two", "2"));
        producer.RawProduce(
            [0x03, 0x03, 0x03, 0x03, 0x03],
            (_, increment) => Interlocked.Add(ref produced, increment),
            (_, increment) => Interlocked.Add(ref errors, increment),
            1,
            envelope => envelope.AddHeader("x-custom", "test 3").AddHeader("two", "2"));

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
    public async Task RawProduce_ShouldProduceStreamUsingCallbacksWithState()
    {
        int produced = 0;
        int errors = 0;

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

        KafkaProducer producer = (KafkaProducer)Helper.GetProducerForEndpoint(DefaultTopicName);

        producer.RawProduce(
            new MemoryStream([0x01, 0x01, 0x01, 0x01, 0x01]),
            (_, increment) => Interlocked.Add(ref produced, increment),
            (_, increment) => Interlocked.Add(ref errors, increment),
            1);
        producer.RawProduce(
            new MemoryStream([0x02, 0x02, 0x02, 0x02, 0x02]),
            (_, increment) => Interlocked.Add(ref produced, increment),
            (_, increment) => Interlocked.Add(ref errors, increment),
            1);
        producer.RawProduce(
            new MemoryStream([0x03, 0x03, 0x03, 0x03, 0x03]),
            (_, increment) => Interlocked.Add(ref produced, increment),
            (_, increment) => Interlocked.Add(ref errors, increment),
            1);

        produced.ShouldBeLessThan(3);

        await AsyncTestingUtil.WaitAsync(() => produced == 3);

        produced.ShouldBe(3);
        errors.ShouldBe(0);

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].Value.ShouldBe([0x01, 0x01, 0x01, 0x01, 0x01]);
        messages[1].Value.ShouldBe([0x02, 0x02, 0x02, 0x02, 0x02]);
        messages[2].Value.ShouldBe([0x03, 0x03, 0x03, 0x03, 0x03]);
    }

    [Fact]
    public async Task RawProduce_ShouldProduceStreamUsingCallbacksWithStateConfiguringEnvelope()
    {
        int produced = 0;
        int errors = 0;

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

        KafkaProducer producer = (KafkaProducer)Helper.GetProducerForEndpoint(DefaultTopicName);

        producer.RawProduce(
            new MemoryStream([0x01, 0x01, 0x01, 0x01, 0x01]),
            (_, increment) => Interlocked.Add(ref produced, increment),
            (_, increment) => Interlocked.Add(ref errors, increment),
            1,
            envelope => envelope.AddHeader("x-custom", "test 1").AddHeader("two", "2"));
        producer.RawProduce(
            new MemoryStream([0x02, 0x02, 0x02, 0x02, 0x02]),
            (_, increment) => Interlocked.Add(ref produced, increment),
            (_, increment) => Interlocked.Add(ref errors, increment),
            1,
            envelope => envelope.AddHeader("x-custom", "test 2").AddHeader("two", "2"));
        producer.RawProduce(
            new MemoryStream([0x03, 0x03, 0x03, 0x03, 0x03]),
            (_, increment) => Interlocked.Add(ref produced, increment),
            (_, increment) => Interlocked.Add(ref errors, increment),
            1,
            envelope => envelope.AddHeader("x-custom", "test 3").AddHeader("two", "2"));

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
    public async Task RawProduce_ShouldProduceEnvelopeUsingCallbacks()
    {
        int produced = 0;
        int errors = 0;

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

        KafkaProducer producer = (KafkaProducer)Helper.GetProducerForEndpoint(DefaultTopicName);

        producer.RawProduce(
            producer.EnvelopeFactory.Create(new MemoryStream([0x01, 0x01, 0x01, 0x01, 0x01]))
                .AddHeader("x-custom", "test 1").AddHeader("two", "2"),
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        producer.RawProduce(
            producer.EnvelopeFactory.Create(new MemoryStream([0x02, 0x02, 0x02, 0x02, 0x02]))
                .AddHeader("x-custom", "test 2").AddHeader("two", "2"),
            _ => Interlocked.Increment(ref produced),
            _ => Interlocked.Increment(ref errors));
        producer.RawProduce(
            envelope: producer.EnvelopeFactory.Create(new MemoryStream([0x03, 0x03, 0x03, 0x03, 0x03]))
                .AddHeader("x-custom", "test 3").AddHeader("two", "2"),
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
    public async Task RawProduce_ShouldProduceEnvelopeUsingCallbacksWithState()
    {
        int produced = 0;
        int errors = 0;

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

        KafkaProducer producer = (KafkaProducer)Helper.GetProducerForEndpoint(DefaultTopicName);

        producer.RawProduce(
            producer.EnvelopeFactory.Create(new MemoryStream([0x01, 0x01, 0x01, 0x01, 0x01]))
                .AddHeader("x-custom", "test 1").AddHeader("two", "2"),
            (_, increment) => Interlocked.Add(ref produced, increment),
            (_, increment) => Interlocked.Add(ref errors, increment),
            1);
        producer.RawProduce(
            producer.EnvelopeFactory.Create(new MemoryStream([0x02, 0x02, 0x02, 0x02, 0x02]))
                .AddHeader("x-custom", "test 2").AddHeader("two", "2"),
            (_, increment) => Interlocked.Add(ref produced, increment),
            (_, increment) => Interlocked.Add(ref errors, increment),
            1);
        producer.RawProduce(
            envelope: producer.EnvelopeFactory.Create(new MemoryStream([0x03, 0x03, 0x03, 0x03, 0x03]))
                .AddHeader("x-custom", "test 3").AddHeader("two", "2"),
            (_, increment) => Interlocked.Add(ref produced, increment),
            (_, increment) => Interlocked.Add(ref errors, increment),
            1);

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
    public async Task RawProduce_ShouldSetKafkaKeyFromEnvelope()
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

        producer.RawProduce(
            BytesUtil.GetRandomBytes(),
            envelope => envelope.SetKafkaRawKey("1001"u8.ToArray()));
        producer.RawProduce(
            BytesUtil.GetRandomBytes(),
            envelope => envelope.SetKafkaRawKey("2002"u8.ToArray()));
        producer.RawProduce(
            BytesUtil.GetRandomBytes(),
            envelope => envelope.SetKafkaRawKey("3003"u8.ToArray()));

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].Key.ShouldBe("1001"u8.ToArray());
        messages[1].Key.ShouldBe("2002"u8.ToArray());
        messages[2].Key.ShouldBe("3003"u8.ToArray());
    }
}
