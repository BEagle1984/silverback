// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class OutboundMessageEnrichmentFixture
{
    [Fact]
    public async Task AddHeader_ShouldAddStaticHeader()
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
                            producer => producer.Produce<IIntegrationEvent>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName)
                                    .AddHeader("one", 1)
                                    .AddHeader("two", 2))))
                .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne());

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(1);
        Helper.Spy.OutboundEnvelopes[0].Headers.ShouldContain(header => header.Name == "one" && header.Value == "1");
        Helper.Spy.OutboundEnvelopes[0].Headers.ShouldContain(header => header.Name == "two" && header.Value == "2");
    }

    [Fact]
    public async Task AddHeader_ShouldAddHeaderByMessageType()
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
                                    .AddHeader<TestEventOne>("x-something", "one")
                                    .AddHeader<TestEventTwo>("x-something", "two"))))
                .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne());
        await publisher.PublishEventAsync(new TestEventTwo());
        await publisher.PublishEventAsync(new TestEventThree());

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.OutboundEnvelopes[0].Headers.ShouldContain(header => header.Name == "x-something" && header.Value == "one");
        Helper.Spy.OutboundEnvelopes[1].Headers.ShouldContain(header => header.Name == "x-something" && header.Value == "two");
        Helper.Spy.OutboundEnvelopes[2].Headers.ShouldNotContain(header => header.Name == "x-something");
    }

    [Fact]
    public async Task AddHeader_ShouldAddHeaderWithValueFunctionBasedOnMessage()
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
                                    .AddHeader<TestEventOne>("x-something", message => message?.ContentEventOne))))
                .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "two" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "three" });
        await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = "four" });

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(4);
        Helper.Spy.OutboundEnvelopes[0].Headers.ShouldContain(header => header.Name == "x-something" && header.Value == "one");
        Helper.Spy.OutboundEnvelopes[1].Headers.ShouldContain(header => header.Name == "x-something" && header.Value == "two");
        Helper.Spy.OutboundEnvelopes[2].Headers.ShouldContain(header => header.Name == "x-something" && header.Value == "three");
        Helper.Spy.OutboundEnvelopes[3].Headers.ShouldNotContain(header => header.Name == "x-something");
    }

    [Fact]
    public async Task AddHeader_ShouldAddHeaderWithValueFunctionBasedOnEnvelope()
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
                                    .AddHeader<TestEventOne>("x-something", envelope => envelope.Message?.ContentEventOne))))
                .AddIntegrationSpy());

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "one" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "two" });
        await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = "three" });
        await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = "four" });

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(4);
        Helper.Spy.OutboundEnvelopes[0].Headers.ShouldContain(header => header.Name == "x-something" && header.Value == "one");
        Helper.Spy.OutboundEnvelopes[1].Headers.ShouldContain(header => header.Name == "x-something" && header.Value == "two");
        Helper.Spy.OutboundEnvelopes[2].Headers.ShouldContain(header => header.Name == "x-something" && header.Value == "three");
        Helper.Spy.OutboundEnvelopes[3].Headers.ShouldNotContain(header => header.Name == "x-something");
    }

    [Fact]
    public async Task AddHeader_ShouldAddHeader_WhenProducingViaProducer()
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
                                    .AddHeader("x-something", envelope => envelope.Message?.ContentEventOne))))
                .AddIntegrationSpy());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "one" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "two" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "three" });

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.OutboundEnvelopes[0].Headers.ShouldContain(header => header.Name == "x-something" && header.Value == "one");
        Helper.Spy.OutboundEnvelopes[1].Headers.ShouldContain(header => header.Name == "x-something" && header.Value == "two");
        Helper.Spy.OutboundEnvelopes[2].Headers.ShouldContain(header => header.Name == "x-something" && header.Value == "three");
    }

    [Fact]
    public async Task AddHeader_ShouldAddHeader_WhenProducingViaProducerWithCallbacks()
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
                            producer => producer.Produce<TestEventOne>(
                                endpoint => endpoint
                                    .ProduceTo(DefaultTopicName)
                                    .AddHeader("x-something", envelope => envelope.Message?.ContentEventOne))))
                .AddIntegrationSpy());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        producer.Produce(
            new TestEventOne { ContentEventOne = "one" },
            null,
            _ =>
            {
            },
            _ =>
            {
            });
        producer.Produce(
            new TestEventOne { ContentEventOne = "two" },
            null,
            _ =>
            {
            },
            _ =>
            {
            });
        producer.Produce(
            new TestEventOne { ContentEventOne = "three" },
            null,
            _ =>
            {
            },
            _ =>
            {
            });

        Helper.Spy.OutboundEnvelopes.Count.ShouldBe(3);
        Helper.Spy.OutboundEnvelopes[0].Headers.ShouldContain(header => header.Name == "x-something" && header.Value == "one");
        Helper.Spy.OutboundEnvelopes[1].Headers.ShouldContain(header => header.Name == "x-something" && header.Value == "two");
        Helper.Spy.OutboundEnvelopes[2].Headers.ShouldContain(header => header.Name == "x-something" && header.Value == "three");
    }
}
