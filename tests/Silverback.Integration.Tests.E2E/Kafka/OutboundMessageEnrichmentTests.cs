// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.Enrichers;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class OutboundMessageEnrichmentTests : KafkaTestFixture
    {
        public OutboundMessageEnrichmentTests(ITestOutputHelper testOutputHelper)
            : base(testOutputHelper)
        {
        }

        [Fact]
        public async Task AddHeader_StaticValues_HeadersAdded()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .AddHeader("one", 1)
                                        .AddHeader("two", 2)))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne());

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(1);
            Helper.Spy.OutboundEnvelopes[0].Headers.Should()
                .ContainSingle(header => header.Name == "one" && header.Value == "1");
            Helper.Spy.OutboundEnvelopes[0].Headers.Should()
                .ContainSingle(header => header.Name == "two" && header.Value == "2");
        }

        [Fact]
        public async Task AddHeader_StaticValuesUsingLegacyConfiguration_HeadersAdded()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddEndpoints(
                            endpoints => endpoints
                                .AddOutbound<IIntegrationEvent>(
                                    new KafkaProducerEndpoint(DefaultTopicName)
                                    {
                                        Configuration = new KafkaProducerConfig
                                        {
                                            BootstrapServers = "PLAINTEXT://tests"
                                        },
                                        MessageEnrichers = new List<IOutboundMessageEnricher>
                                        {
                                            new GenericOutboundHeadersEnricher("one", 1),
                                            new GenericOutboundHeadersEnricher("two", 2)
                                        }
                                    }))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
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
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .AddHeader<TestEventOne>("x-something", "value")))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
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
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .AddHeader<TestEventOne>(
                                            "x-something",
                                            envelope => envelope.Message?.Content)))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne { Content = "one" });
            await publisher.PublishAsync(new TestEventOne { Content = "two" });
            await publisher.PublishAsync(new TestEventOne { Content = "three" });
            await publisher.PublishAsync(new TestEventTwo { Content = "four" });

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(4);
            Helper.Spy.OutboundEnvelopes[0].Headers.Should()
                .ContainSingle(header => header.Name == "x-something" && header.Value == "one");
            Helper.Spy.OutboundEnvelopes[1].Headers.Should()
                .ContainSingle(header => header.Name == "x-something" && header.Value == "two");
            Helper.Spy.OutboundEnvelopes[2].Headers.Should()
                .ContainSingle(header => header.Name == "x-something" && header.Value == "three");
            Helper.Spy.OutboundEnvelopes[3].Headers.Should()
                .NotContain(header => header.Name == "x-something");
        }

        [Fact]
        public async Task WithMessageId_HeaderAdded()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .WithMessageId<TestEventOne>(envelope => envelope.Message?.Content)))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne { Content = "one" });
            await publisher.PublishAsync(new TestEventOne { Content = "two" });
            await publisher.PublishAsync(new TestEventOne { Content = "three" });
            await publisher.PublishAsync(new TestEventTwo { Content = "four" });

            Helper.Spy.OutboundEnvelopes.Should().HaveCount(4);
            Helper.Spy.OutboundEnvelopes[0].Headers.Should()
                .ContainSingle(header => header.Name == "x-message-id" && header.Value == "one");
            Helper.Spy.OutboundEnvelopes[1].Headers.Should()
                .ContainSingle(header => header.Name == "x-message-id" && header.Value == "two");
            Helper.Spy.OutboundEnvelopes[2].Headers.Should()
                .ContainSingle(header => header.Name == "x-message-id" && header.Value == "three");
            Helper.Spy.OutboundEnvelopes[3].Headers.Should()
                .ContainSingle(header => header.Name == "x-message-id" && header.Value != "four");
        }

        [Fact]
        public async Task WithKafkaKey_MessageKeySet()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<TestEventOne>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .WithKafkaKey<TestEventOne>(envelope => envelope.Message?.Content)))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne { Content = "one" });
            await publisher.PublishAsync(new TestEventOne { Content = "two" });
            await publisher.PublishAsync(new TestEventOne { Content = "three" });

            var messages = DefaultTopic.GetAllMessages();
            messages.Should().HaveCount(3);
            messages[0].Key.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("one"));
            messages[1].Key.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("two"));
            messages[2].Key.Should().BeEquivalentTo(Encoding.UTF8.GetBytes("three"));
        }

        [Fact]
        public async Task WithKafkaKey_Null_NullMessageKeySet()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(
                            options => options.AddMockedKafka(
                                mockedKafkaOptions => mockedKafkaOptions.WithDefaultPartitionsCount(1)))
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<TestEventOne>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .WithKafkaKey<TestEventOne>(_ => null)))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var publisher = Host.ScopedServiceProvider.GetRequiredService<IEventPublisher>();
            await publisher.PublishAsync(new TestEventOne { Content = "one" });
            await publisher.PublishAsync(new TestEventOne { Content = "two" });
            await publisher.PublishAsync(new TestEventOne { Content = "three" });

            var messages = DefaultTopic.GetAllMessages();
            messages.Should().HaveCount(3);
            messages[0].Key.Should().BeNull();
            messages[1].Key.Should().BeNull();
            messages[2].Key.Should().BeNull();
        }

        [Fact]
        public async Task AddHeader_ProducingViaProducer_HeaderAdded()
        {
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .AddHeader<TestEventOne>(
                                            "x-something",
                                            envelope => envelope.Message?.Content)))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);
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
            Host.ConfigureServices(
                    services => services
                        .AddLogging()
                        .AddSilverback()
                        .UseModel()
                        .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                        .AddKafkaEndpoints(
                            endpoints => endpoints
                                .Configure(
                                    config =>
                                    {
                                        config.BootstrapServers = "PLAINTEXT://tests";
                                    })
                                .AddOutbound<IIntegrationEvent>(
                                    endpoint => endpoint
                                        .ProduceTo(DefaultTopicName)
                                        .AddHeader<TestEventOne>(
                                            "x-something",
                                            envelope => envelope.Message?.Content)))
                        .AddIntegrationSpyAndSubscriber())
                .Run();

            var producer = Helper.Broker.GetProducer(DefaultTopicName);
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
}
