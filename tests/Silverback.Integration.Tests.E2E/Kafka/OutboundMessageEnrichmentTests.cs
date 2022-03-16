// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class OutboundMessageEnrichmentTests : KafkaTestFixture
{
    public OutboundMessageEnrichmentTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task AddHeader_StaticValues_HeadersAdded()
    {
        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .AddHeader("one", 1)
                                    .AddHeader("two", 2)))
                    .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne());

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
        Helper.Spy.OutboundEnvelopes[0].Headers.Should()
            .ContainSingle(header => header.Name == "one" && header.Value == "1");
        Helper.Spy.OutboundEnvelopes[0].Headers.Should()
            .ContainSingle(header => header.Name == "two" && header.Value == "2");
    }

    [Fact]
    public async Task AddHeader_SpecificMessageTypeOnly_HeadersAdded()
    {
        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .AddHeader<TestEventOne>("x-something", "value")))
                    .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne());
        await publisher.PublishAsync(new TestEventTwo());

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(2);
        Helper.Spy.OutboundEnvelopes[0].Headers.Should()
            .ContainSingle(header => header.Name == "x-something" && header.Value == "value");
        Helper.Spy.OutboundEnvelopes[1].Headers.Should()
            .NotContain(header => header.Name == "x-something");
    }

    [Fact]
    public async Task AddHeader_ValueFunction_HeaderAdded()
    {
        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .AddHeader<TestEventOne>(
                                        "x-something",
                                        envelope => envelope.Message?.Content)))
                    .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "one" });
        await publisher.PublishAsync(new TestEventOne { Content = "two" });
        await publisher.PublishAsync(new TestEventOne { Content = "three" });
        await publisher.PublishAsync(new TestEventTwo { Content = "four" });

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(4);
        Helper.Spy.OutboundEnvelopes[0].Headers.Should().ContainSingle(header => header.Name == "x-something" && header.Value == "one");
        Helper.Spy.OutboundEnvelopes[1].Headers.Should().ContainSingle(header => header.Name == "x-something" && header.Value == "two");
        Helper.Spy.OutboundEnvelopes[2].Headers.Should().ContainSingle(header => header.Name == "x-something" && header.Value == "three");
        Helper.Spy.OutboundEnvelopes[3].Headers.Should().NotContain(header => header.Name == "x-something");
    }

    [Fact]
    public async Task WithMessageId_HeaderAdded()
    {
        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .WithMessageId<TestEventOne>(envelope => envelope.Message?.Content)))
                    .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "one" });
        await publisher.PublishAsync(new TestEventOne { Content = "two" });
        await publisher.PublishAsync(new TestEventOne { Content = "three" });
        await publisher.PublishAsync(new TestEventTwo { Content = "four" });

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(4);
        Helper.Spy.OutboundEnvelopes[0].Headers.Should().ContainSingle(header => header.Name == "x-message-id" && header.Value == "one");
        Helper.Spy.OutboundEnvelopes[1].Headers.Should().ContainSingle(header => header.Name == "x-message-id" && header.Value == "two");
        Helper.Spy.OutboundEnvelopes[2].Headers.Should().ContainSingle(header => header.Name == "x-message-id" && header.Value == "three");
        Helper.Spy.OutboundEnvelopes[3].Headers.Should().ContainSingle(header => header.Name == "x-message-id" && header.Value != "four");
    }

    [Fact]
    public async Task WithKafkaKey_MessageKeySet()
    {
        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<TestEventOne>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .WithKafkaKey(message => message?.Content)))
                    .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "one" });
        await publisher.PublishAsync(new TestEventOne { Content = "two" });
        await publisher.PublishAsync(new TestEventOne { Content = "three" });

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(3);
        messages[0].Key.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("one"));
        messages[1].Key.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("two"));
        messages[2].Key.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("three"));
    }

    [Fact]
    public async Task WithKafkaKey_FunctionReturningNull_NullMessageKeySet()
    {
        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)))
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<TestEventOne>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .WithKafkaKey((TestEventOne? _) => null)))
                    .AddIntegrationSpyAndSubscriber());

        IEventPublisher publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
        await publisher.PublishAsync(new TestEventOne { Content = "one" });
        await publisher.PublishAsync(new TestEventOne { Content = "two" });
        await publisher.PublishAsync(new TestEventOne { Content = "three" });

        IReadOnlyList<Message<byte[]?, byte[]?>> messages = DefaultTopic.GetAllMessages();
        messages.Should().HaveCount(3);
        messages[0].Key.Should().BeNull();
        messages[1].Key.Should().BeNull();
        messages[2].Key.Should().BeNull();
    }

    [Fact]
    public async Task AddHeader_EnvelopeBasedProducingViaProducer_HeaderAdded()
    {
        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .AddHeader<TestEventOne>(
                                        "x-something",
                                        envelope => envelope.Message?.Content)))
                    .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.Broker.GetProducer(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { Content = "one" });
        await producer.ProduceAsync(new TestEventOne { Content = "two" });
        await producer.ProduceAsync(new TestEventOne { Content = "three" });

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.OutboundEnvelopes[0].Headers.Should()
            .ContainSingle(header => header.Name == "x-something" && header.Value == "one");
        Helper.Spy.OutboundEnvelopes[1].Headers.Should()
            .ContainSingle(header => header.Name == "x-something" && header.Value == "two");
        Helper.Spy.OutboundEnvelopes[2].Headers.Should()
            .ContainSingle(header => header.Name == "x-something" && header.Value == "three");
    }

    [Fact]
    public async Task AddHeader_MessageBasedProducingViaProducer_HeaderAdded()
    {
        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .AddHeader<TestEventOne>(
                                        "x-something",
                                        message => message?.Content)))
                    .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.Broker.GetProducer(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { Content = "one" });
        await producer.ProduceAsync(new TestEventOne { Content = "two" });
        await producer.ProduceAsync(new TestEventOne { Content = "three" });

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.OutboundEnvelopes[0].Headers.Should()
            .ContainSingle(header => header.Name == "x-something" && header.Value == "one");
        Helper.Spy.OutboundEnvelopes[1].Headers.Should()
            .ContainSingle(header => header.Name == "x-something" && header.Value == "two");
        Helper.Spy.OutboundEnvelopes[2].Headers.Should()
            .ContainSingle(header => header.Name == "x-something" && header.Value == "three");
    }

    [Fact]
    public async Task AddHeader_ProducingViaProducerWithCallbacks_HeaderAdded()
    {
        Host.ConfigureServicesAndRun(
                services => services
                    .AddLogging()
                    .AddSilverback()
                    .UseModel()
                    .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                    .AddKafkaEndpoints(
                        endpoints => endpoints
                            .ConfigureClient(configuration => configuration.WithBootstrapServers("PLAINTEXT://e2e"))
                            .AddOutbound<IIntegrationEvent>(
                                producer => producer
                                    .ProduceTo(DefaultTopicName)
                                    .AddHeader<TestEventOne>(
                                        "x-something",
                                        message => message?.Content)))
                    .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.Broker.GetProducer(DefaultTopicName);
        await producer.ProduceAsync(
            new TestEventOne { Content = "one" },
            null,
            _ =>
            {
            },
            _ =>
            {
            });
        await producer.ProduceAsync(
            new TestEventOne { Content = "two" },
            null,
            _ =>
            {
            },
            _ =>
            {
            });
        await producer.ProduceAsync(
            new TestEventOne { Content = "three" },
            null,
            _ =>
            {
            },
            _ =>
            {
            });

        Helper.Spy.OutboundEnvelopes.Should().HaveCount(3);
        Helper.Spy.OutboundEnvelopes[0].Headers.Should()
            .ContainSingle(header => header.Name == "x-something" && header.Value == "one");
        Helper.Spy.OutboundEnvelopes[1].Headers.Should()
            .ContainSingle(header => header.Name == "x-something" && header.Value == "two");
        Helper.Spy.OutboundEnvelopes[2].Headers.Should()
            .ContainSingle(header => header.Name == "x-something" && header.Value == "three");
    }
}
