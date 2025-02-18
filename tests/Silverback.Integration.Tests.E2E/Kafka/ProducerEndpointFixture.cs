// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class ProducerEndpointFixture : KafkaFixture
{
    public ProducerEndpointFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldSerializeAndProduce()
    {
        TestEventOne message = new() { ContentEventOne = "Hello E2E!" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(producer => producer.Produce<IIntegrationEvent>(endpoint => endpoint.ProduceTo(DefaultTopicName)))));

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(message);

        DefaultTopic.MessagesCount.ShouldBe(1);
        DefaultTopic.GetAllMessages()[0].Value.ShouldBe(rawMessage);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldSerializeAndProduce_WhenMultipleProducersForDifferentMessagesAreConfigured()
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
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1")))
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventTwo>(
                                    endpoint => endpoint.ProduceTo("topic2").SerializeAsJson(
                                        serializer => serializer
                                            .Configure(
                                                options =>
                                                {
                                                    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                                                }))))));

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
            await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = $"{i}" });
            await publisher.PublishEventAsync(new TestEventThree { ContentEventThree = $"{i}" });
        }

        Host.ServiceProvider.GetRequiredService<IProducerCollection>().Count.ShouldBe(2);
        Helper.GetTopic("topic1").GetAllMessages().GetContentAsString().ShouldBe(
            [
                "{\"ContentEventOne\":\"1\"}",
                "{\"ContentEventOne\":\"2\"}",
                "{\"ContentEventOne\":\"3\"}",
                "{\"ContentEventOne\":\"4\"}",
                "{\"ContentEventOne\":\"5\"}"
            ],
            true);
        Helper.GetTopic("topic2").GetAllMessages().GetContentAsString().ShouldBe(
            [
                "{\"contentEventTwo\":\"1\"}",
                "{\"contentEventTwo\":\"2\"}",
                "{\"contentEventTwo\":\"3\"}",
                "{\"contentEventTwo\":\"4\"}",
                "{\"contentEventTwo\":\"5\"}"
            ],
            true);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldSerializeAndProduce_WhenSingleProducerHandlesMultipleTypes()
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
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1"))
                                .Produce<TestEventTwo>(
                                    endpoint => endpoint.ProduceTo("topic2").SerializeAsJson(
                                        serializer => serializer
                                            .Configure(
                                                options =>
                                                {
                                                    options.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                                                }))))));

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
            await publisher.PublishEventAsync(new TestEventTwo { ContentEventTwo = $"{i}" });
            await publisher.PublishEventAsync(new TestEventThree { ContentEventThree = $"{i}" });
        }

        Host.ServiceProvider.GetRequiredService<IProducerCollection>().Count.ShouldBe(2);
        Helper.GetTopic("topic1").GetAllMessages().GetContentAsString().ShouldBe(
            [
                "{\"ContentEventOne\":\"1\"}",
                "{\"ContentEventOne\":\"2\"}",
                "{\"ContentEventOne\":\"3\"}",
                "{\"ContentEventOne\":\"4\"}",
                "{\"ContentEventOne\":\"5\"}"
            ],
            true);
        Helper.GetTopic("topic2").GetAllMessages().GetContentAsString().ShouldBe(
            [
                "{\"contentEventTwo\":\"1\"}",
                "{\"contentEventTwo\":\"2\"}",
                "{\"contentEventTwo\":\"3\"}",
                "{\"contentEventTwo\":\"4\"}",
                "{\"contentEventTwo\":\"5\"}"
            ],
            true);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldSerializeAndProduce_WhenBroadcastingToMultipleTopics()
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
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic1"))
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic2")))
                        .AddProducer(
                            producer => producer
                                .Produce<TestEventOne>(endpoint => endpoint.ProduceTo("topic3")))));

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        for (int i = 1; i <= 5; i++)
        {
            await publisher.PublishEventAsync(new TestEventOne { ContentEventOne = $"{i}" });
        }

        Helper.GetTopic("topic1").GetAllMessages().GetContentAsString().ShouldBe(
            [
                "{\"ContentEventOne\":\"1\"}",
                "{\"ContentEventOne\":\"2\"}",
                "{\"ContentEventOne\":\"3\"}",
                "{\"ContentEventOne\":\"4\"}",
                "{\"ContentEventOne\":\"5\"}"
            ],
            true);
        Helper.GetTopic("topic2").GetAllMessages().GetContentAsString().ShouldBe(
            [
                "{\"ContentEventOne\":\"1\"}",
                "{\"ContentEventOne\":\"2\"}",
                "{\"ContentEventOne\":\"3\"}",
                "{\"ContentEventOne\":\"4\"}",
                "{\"ContentEventOne\":\"5\"}"
            ],
            true);
        Helper.GetTopic("topic3").GetAllMessages().GetContentAsString().ShouldBe(
            [
                "{\"ContentEventOne\":\"1\"}",
                "{\"ContentEventOne\":\"2\"}",
                "{\"ContentEventOne\":\"3\"}",
                "{\"ContentEventOne\":\"4\"}",
                "{\"ContentEventOne\":\"5\"}"
            ],
            true);
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldProduceCustomHeaders()
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
                                .Produce<IIntegrationEvent>(
                                    endpoint => endpoint.ProduceTo(DefaultTopicName)
                                        .AddHeader<TestEventWithHeaders>("x-content", envelope => envelope.Message?.Content)
                                        .AddHeader<TestEventOne>("x-content-nope", envelope => envelope.Message?.ContentEventOne)
                                        .AddHeader("x-static", 42)))));

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();

        await publisher.PublishEventAsync(
            new TestEventWithHeaders
            {
                Content = "Hello E2E!",
                CustomHeader = "Hello header!",
                CustomHeader2 = false
            });

        Message<byte[]?, byte[]?> message = DefaultTopic.GetAllMessages().Single();
        message.GetContentAsString().ShouldBe("{\"Content\":\"Hello E2E!\"}");

        message.Headers.ShouldContain(header => header.Key == "x-content" && header.GetValueAsString() == "Hello E2E!");
        message.Headers.ShouldContain(header => header.Key == "x-static" && header.GetValueAsString() == "42");
        message.Headers.ShouldContain(header => header.Key == "x-custom-header" && header.GetValueAsString() == "Hello header!");
        message.Headers.ShouldContain(header => header.Key == "x-custom-header2" && header.GetValueAsString() == "False");
        message.Headers.ShouldNotContain(header => header.Key == "x-content-nope");
    }

    [Fact]
    public async Task ProducerEndpoint_ShouldNotSetupRouteButStillWork_WhenMessageTypeIsNotSpecified()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddProducer(producer => producer.Produce(endpoint => endpoint.ProduceTo(DefaultTopicName)))));

        IPublisher publisher = Host.ServiceProvider.GetRequiredService<IPublisher>();
        await publisher.PublishEventAsync(new TestEventOne());

        Helper.GetTopic(DefaultTopicName, "PLAINTEXT://e2e").MessagesCount.ShouldBe(0); // Needed to force topic creation
        DefaultTopic.MessagesCount.ShouldBe(0);

        IProducer producer = Host.ServiceProvider.GetRequiredService<IProducerCollection>().GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());

        DefaultTopic.MessagesCount.ShouldBe(1);
    }
}
