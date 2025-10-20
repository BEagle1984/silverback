// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ProducerTests
{
    [Fact]
    public async Task Produce_ShouldProduceMessage()
    {
        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName))))
            .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            producer.Produce(new TestEventOne { ContentEventOne = $"{i}" });
        }

        DefaultTopic.MessagesCount.ShouldBe(3);
        DefaultTopic.GetAllMessages().GetContentAsString().ShouldBe(
            [
                "{\"ContentEventOne\":\"1\"}",
                "{\"ContentEventOne\":\"2\"}",
                "{\"ContentEventOne\":\"3\"}"
            ],
            true);
    }

    [Fact]
    public async Task Produce_ShouldProduceMessageConfiguringEnvelope()
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

        for (int i = 1; i <= 3; i++)
        {
            int iLocal = i;
            producer.Produce(
                new TestEventOne { ContentEventOne = $"Hello E2E {i}!" },
                envelope => envelope.AddHeader("x-custom", $"test {iLocal}").AddHeader("two", "2"));
        }

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].GetContentAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E 1!\"}");
        messages[0].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 1");
        messages[1].GetContentAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E 2!\"}");
        messages[1].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 2");
        messages[2].GetContentAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E 3!\"}");
        messages[2].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 3");
    }

    [Fact]
    public async Task Produce_ShouldProduceEnvelope()
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

        KafkaProducer producer = (KafkaProducer)Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            producer.Produce(
                producer.EnvelopeFactory.Create(new TestEventOne { ContentEventOne = $"Hello E2E {i}!" })
                    .AddHeader("x-custom", $"test {i}")
                    .AddHeader("two", "2"));
        }

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].GetContentAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E 1!\"}");
        messages[0].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 1");
        messages[1].GetContentAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E 2!\"}");
        messages[1].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 2");
        messages[2].GetContentAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E 3!\"}");
        messages[2].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 3");
    }

    [Fact]
    public async Task Produce_ShouldProduceMessageUsingCallbacks()
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

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            producer.Produce(
                new TestEventOne { ContentEventOne = $"Hello E2E {i}!" },
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));
        }

        produced.ShouldBeLessThan(3);

        await AsyncTestingUtil.WaitAsync(() => produced == 3);

        produced.ShouldBe(3);
        errors.ShouldBe(0);

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].GetContentAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E 1!\"}");
        messages[1].GetContentAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E 2!\"}");
        messages[2].GetContentAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E 3!\"}");
    }

    [Fact]
    public async Task Produce_ShouldProduceMessageUsingCallbacksConfiguringEnvelope()
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

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            int iLocal = i;
            producer.Produce(
                new TestEventOne { ContentEventOne = $"Hello E2E {i}!" },
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors),
                envelope => envelope.AddHeader("x-custom", $"test {iLocal}"));
        }

        produced.ShouldBeLessThan(3);

        await AsyncTestingUtil.WaitAsync(() => produced == 3);

        produced.ShouldBe(3);
        errors.ShouldBe(0);

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].GetContentAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E 1!\"}");
        messages[0].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 1");
        messages[1].GetContentAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E 2!\"}");
        messages[1].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 2");
        messages[2].GetContentAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E 3!\"}");
        messages[2].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 3");
    }

    [Fact]
    public async Task Produce_ShouldProduceMessageUsingCallbacksWithState()
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

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            producer.Produce(
                new TestEventOne { ContentEventOne = $"Hello E2E {i}!" },
                (_, increment) => Interlocked.Add(ref produced, increment),
                (_, increment) => Interlocked.Add(ref errors, increment),
                1);
        }

        produced.ShouldBeLessThan(3);

        await AsyncTestingUtil.WaitAsync(() => produced == 3);

        produced.ShouldBe(3);
        errors.ShouldBe(0);

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].GetContentAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E 1!\"}");
        messages[1].GetContentAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E 2!\"}");
        messages[2].GetContentAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E 3!\"}");
    }

    [Fact]
    public async Task Produce_ShouldProduceMessageUsingCallbacksWithStateConfiguringEnvelope()
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

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 3; i++)
        {
            int iLocal = i;
            producer.Produce(
                new TestEventOne { ContentEventOne = $"Hello E2E {i}!" },
                (_, increment) => Interlocked.Add(ref produced, increment),
                (_, increment) => Interlocked.Add(ref errors, increment),
                1,
                envelope => envelope.AddHeader("x-custom", $"test {iLocal}"));
        }

        produced.ShouldBeLessThan(3);

        await AsyncTestingUtil.WaitAsync(() => produced == 3);

        produced.ShouldBe(3);
        errors.ShouldBe(0);

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].GetContentAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E 1!\"}");
        messages[0].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 1");
        messages[1].GetContentAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E 2!\"}");
        messages[1].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 2");
        messages[2].GetContentAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E 3!\"}");
        messages[2].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 3");
    }

    [Fact]
    public async Task Produce_ShouldProduceEnvelopeUsingCallbacks()
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

        for (int i = 1; i <= 3; i++)
        {
            producer.Produce(
                producer.EnvelopeFactory.Create(new TestEventOne { ContentEventOne = $"Hello E2E {i}!" })
                    .AddHeader("x-custom", $"test {i}"),
                _ => Interlocked.Increment(ref produced),
                _ => Interlocked.Increment(ref errors));
        }

        produced.ShouldBeLessThan(3);

        await AsyncTestingUtil.WaitAsync(() => produced == 3);

        produced.ShouldBe(3);
        errors.ShouldBe(0);

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].GetContentAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E 1!\"}");
        messages[0].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 1");
        messages[1].GetContentAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E 2!\"}");
        messages[1].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 2");
        messages[2].GetContentAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E 3!\"}");
        messages[2].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 3");
    }

    [Fact]
    public async Task Produce_ShouldProduceEnvelopeUsingCallbacksWithState()
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

        for (int i = 1; i <= 3; i++)
        {
            producer.Produce(
                producer.EnvelopeFactory.Create(new TestEventOne { ContentEventOne = $"Hello E2E {i}!" })
                    .AddHeader("x-custom", $"test {i}"),
                (_, increment) => Interlocked.Add(ref produced, increment),
                (_, increment) => Interlocked.Add(ref errors, increment),
                1);
        }

        produced.ShouldBeLessThan(3);

        await AsyncTestingUtil.WaitAsync(() => produced == 3);

        produced.ShouldBe(3);
        errors.ShouldBe(0);

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].GetContentAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E 1!\"}");
        messages[0].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 1");
        messages[1].GetContentAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E 2!\"}");
        messages[1].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 2");
        messages[2].GetContentAsString().ShouldBe("{\"ContentEventOne\":\"Hello E2E 3!\"}");
        messages[2].Headers.ShouldContain(header => header.Key == "x-custom" && header.GetValueAsString() == "test 3");
    }

    [Fact]
    public async Task Produce_ShouldSetKafkaKeyFromEnvelope()
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

        producer.Produce(new TestEventOne(), envelope => envelope.SetKafkaKey("1001"));
        producer.Produce(new TestEventOne(), envelope => envelope.SetKafkaKey("2002"));
        producer.Produce(new TestEventOne(), envelope => envelope.SetKafkaKey("3003"));

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].Key.ShouldBe("1001"u8.ToArray());
        messages[1].Key.ShouldBe("2002"u8.ToArray());
        messages[2].Key.ShouldBe("3003"u8.ToArray());
    }

    [Fact]
    public async Task Produce_ShouldSetKafkaKeyFromTombstone()
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

        producer.Produce(new Tombstone<TestEventOne>("1001"));
        producer.Produce(new Tombstone<TestEventOne>("2002"));
        producer.Produce(new Tombstone<TestEventOne>("3003"));

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Count.ShouldBe(3);
        messages[0].Key.ShouldBe("1001"u8.ToArray());
        messages[1].Key.ShouldBe("2002"u8.ToArray());
        messages[2].Key.ShouldBe("3003"u8.ToArray());
    }
}
