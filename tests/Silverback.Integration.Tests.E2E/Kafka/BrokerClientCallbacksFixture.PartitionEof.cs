// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using JetBrains.Annotations;
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
    public async Task PartitionEofCallback_ShouldBeInvokedForAllConsumers()
    {
        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .AddSingletonBrokerClientCallback<KafkaPartitionEofCallback>()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3)))
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            "consumer1",
                            consumer => consumer
                                .WithGroupId("group1")
                                .EnablePartitionEof()
                                .Consume(endpoint => endpoint.ConsumeFrom("topic1")))
                        .AddConsumer(
                            "consumer2",
                            consumer => consumer
                                .WithGroupId("group2")
                                .EnablePartitionEof()
                                .Consume(endpoint => endpoint.ConsumeFrom("topic2"))))
                .AddIntegrationSpyAndSubscriber());

        KafkaPartitionEofCallback callbackKafkaEndOfPartitionReached = (KafkaPartitionEofCallback)Host
            .ScopedServiceProvider
            .GetServices<IBrokerClientCallback>()
            .First(service => service is KafkaPartitionEofCallback);

        await AsyncTestingUtil.WaitAsync(() => callbackKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount == 6);

        IConsumer consumer1 = Helper.GetConsumer("consumer1");
        IConsumer consumer2 = Helper.GetConsumer("consumer2");

        // Stop, produce a few messages and restart the consumers to ensure that the EOF Callback is called only once per partition
        await consumer1.StopAsync();
        await consumer2.StopAsync();

        IProducer producer1 = Helper.GetProducerForEndpoint("topic1");
        await producer1.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 42 });
        await producer1.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 42 });
        IProducer producer2 = Helper.GetProducerForEndpoint("topic2");
        await producer2.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 42 });
        await producer2.ProduceAsync(new TestEventWithKafkaKey { KafkaKey = 42 });

        await consumer1.StartAsync();
        await consumer2.StartAsync();

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        await AsyncTestingUtil.WaitAsync(() => callbackKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount == 8);

        // There are 3 partitions and 1 message will be published, so a total of 4 EOF callbacks per consumer are expected
        callbackKafkaEndOfPartitionReached.AllPartitionsEofCallbackCount.Should().Be(8);
    }

    [UsedImplicitly]
    private sealed class KafkaPartitionEofCallback : IKafkaPartitionEofCallback
    {
        public int AllPartitionsEofCallbackCount => PartitionEofCallbacksDictionary.Sum(pair => pair.Value);

        private ConcurrentDictionary<TopicPartition, int> PartitionEofCallbacksDictionary { get; } = new();

        public void OnEndOfTopicPartitionReached(TopicPartition topicPartition, KafkaConsumer consumer) =>
            PartitionEofCallbacksDictionary.AddOrUpdate(topicPartition, 1, (_, value) => value + 1);
    }
}
