// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Microsoft.Extensions.DependencyInjection;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class AdminClientTests : KafkaTests
{
    private const int CreatedPartitionsCount = 2;

    private const string ProducerName = "producer";

    private const string ConsumerName = "consumer";

    private const string ProducerEndpointName = "producer-endpoint";

    private const string ConsumerEndpointName = "consumer-endpoint";

    public AdminClientTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task GetClientForConsumerEndpoint_ShouldCreateTopicBeforeConnecting_WhenCalledFromClientsConfiguredCallback()
    {
        await ConfigureAndRunAsync();

        DefaultTopic.Partitions.Count.ShouldBe(CreatedPartitionsCount);
    }

    [Theory]
    [InlineData(ProducerEndpointName)]
    [InlineData(DefaultTopicName)]
    public async Task GetClientForProducerEndpoint_ShouldReturnClient_WhenCalledWithEndpointName(string endpointName)
    {
        await ConfigureAndRunAsync();
        IConfluentAdminClientFactory factory = Host.ServiceProvider.GetRequiredService<IConfluentAdminClientFactory>();

        using IAdminClient client = factory.GetClientForProducerEndpoint(endpointName);

        AssertPartitionsCount(client);
    }

    [Theory]
    [InlineData(ConsumerEndpointName)]
    [InlineData(DefaultTopicName)]
    public async Task GetClientForConsumerEndpoint_ShouldReturnClient_WhenCalledWithEndpointNameOrTopicOfMultiTopicEndpoint(string endpointName)
    {
        await ConfigureAndRunAsync();
        IConfluentAdminClientFactory factory = Host.ServiceProvider.GetRequiredService<IConfluentAdminClientFactory>();

        using IAdminClient client = factory.GetClientForConsumerEndpoint(endpointName);

        AssertPartitionsCount(client);
    }

    [Fact]
    public async Task GetClientForProducer_ShouldReturnClient_WhenCalledWithProducerName()
    {
        await ConfigureAndRunAsync();
        IConfluentAdminClientFactory factory = Host.ServiceProvider.GetRequiredService<IConfluentAdminClientFactory>();

        using IAdminClient client = factory.GetClientForProducer(ProducerName);

        AssertPartitionsCount(client);
    }

    [Fact]
    public async Task GetClientForConsumer_ShouldReturnClient_WhenCalledWithConsumerName()
    {
        await ConfigureAndRunAsync();
        IConfluentAdminClientFactory factory = Host.ServiceProvider.GetRequiredService<IConfluentAdminClientFactory>();

        using IAdminClient client = factory.GetClientForConsumer(ConsumerName);

        AssertPartitionsCount(client);
    }

    private static void AssertPartitionsCount(IAdminClient client) =>
        client.GetMetadata(DefaultTopicName, TimeSpan.FromSeconds(1)).Topics.ShouldHaveSingleItem().Partitions.Count
            .ShouldBe(CreatedPartitionsCount);

    private ValueTask ConfigureAndRunAsync() =>
        Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options.AddMockedKafka())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(ProducerName, producer => producer
                    .Produce<TestEventOne>(ProducerEndpointName, endpoint => endpoint.ProduceTo(DefaultTopicName)))
                .AddConsumer(ConsumerName, consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .Consume(ConsumerEndpointName, endpoint => endpoint.ConsumeFrom("other-topic", DefaultTopicName))))
            .AddSingletonBrokerClientCallback<CreateTopicCallback>()
            .AddIntegrationSpyAndSubscriber());

    private sealed class CreateTopicCallback : IBrokerClientsConfiguredCallback
    {
        private readonly IConfluentAdminClientFactory _adminClientFactory;

        public CreateTopicCallback(IConfluentAdminClientFactory adminClientFactory)
        {
            _adminClientFactory = adminClientFactory;
        }

        public async Task OnBrokerClientsConfiguredAsync()
        {
            using IAdminClient client = _adminClientFactory.GetClientForConsumerEndpoint(ConsumerEndpointName);

            await client.CreateTopicsAsync(
            [
                new TopicSpecification
                {
                    Name = DefaultTopicName,
                    NumPartitions = CreatedPartitionsCount
                }
            ]);
        }
    }
}
