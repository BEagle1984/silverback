// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public partial class BrokerClientCallbacksFixture
{
    [Fact]
    public async Task PartitionsAssignedCallback_ShouldSetOffset()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddTransientBrokerClientCallback<ResetOffsetPartitionsAssignedCallback>()
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Message 1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Message 2" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Message 3" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();

        await consumer.Client.DisconnectAsync();
        await consumer.Client.ConnectAsync();

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Message 4" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        Helper.Spy.InboundEnvelopes.Should().HaveCount(7);
    }

    [Fact]
    public async Task PartitionsAssignedCallback_ShouldNotSetOffset_WhenCallbackReturnsNull()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options.AddMockedKafka())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddTransientBrokerClientCallback<NoResetPartitionsAssignedCallback>()
                .AddIntegrationSpyAndSubscriber());

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Message 1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Message 2" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Message 3" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        Helper.Spy.InboundEnvelopes.Should().HaveCount(3);

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();

        await consumer.Client.DisconnectAsync();
        await consumer.Client.ConnectAsync();

        await Helper.WaitUntilConnectedAsync();

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "Message 4" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        Helper.Spy.InboundEnvelopes.Should().HaveCount(4);
    }

    private class ResetOffsetPartitionsAssignedCallback : IKafkaPartitionsAssignedCallback
    {
        public IEnumerable<TopicPartitionOffset> OnPartitionsAssigned(
            IReadOnlyCollection<TopicPartition> topicPartitions,
            KafkaConsumer consumer) =>
            topicPartitions.Select(topicPartition => new TopicPartitionOffset(topicPartition, Offset.Beginning));
    }

    private sealed class NoResetPartitionsAssignedCallback : IKafkaPartitionsAssignedCallback
    {
        public IEnumerable<TopicPartitionOffset>? OnPartitionsAssigned(
            IReadOnlyCollection<TopicPartition> topicPartitions,
            KafkaConsumer consumer) => null;
    }
}
