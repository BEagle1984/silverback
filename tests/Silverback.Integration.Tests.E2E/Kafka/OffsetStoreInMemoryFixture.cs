// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class OffsetStoreInMemoryFixture : KafkaFixture
{
    public OffsetStoreInMemoryFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task OffsetStore_ShouldStoreSubscribedTopicsOffsets()
    {
        int received = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3))
                        .AddInMemoryKafkaOffsetStore())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .DisableOffsetsCommit()
                                .StoreOffsetsClientSide(offsetStore => offsetStore.UseMemory())
                                .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                .AddDelegateSubscriber<TestEventOne>(_ => Interlocked.Increment(ref received))
                .AddIntegrationSpy());

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().First();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 0; i < 5; i++)
        {
            await producer.ProduceAsync(new TestEventOne());
        }

        await AsyncTestingUtil.WaitAsync(() => received >= 5);

        received.Should().Be(5);
        Helper.ConsumerGroups.Should().HaveCount(1);
        Helper.ConsumerGroups.First().CommittedOffsets.Should().BeEmpty();

        await consumer.Client.DisconnectAsync();

        // If offsets are properly stored, those will be used to reposition while reconnecting
        await consumer.Client.ConnectAsync();

        for (int i = 0; i < 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne());
        }

        await AsyncTestingUtil.WaitAsync(() => received >= 8);
        received.Should().Be(8);
    }

    [Fact]
    public async Task OffsetStore_ShouldStoreManuallyAssignedPartitionsOffsets()
    {
        int received = 0;

        await Host.ConfigureServicesAndRunAsync(
            services => services
                .AddLogging()
                .AddSilverback()
                .WithConnectionToMessageBroker(
                    options => options
                        .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3))
                        .AddInMemoryKafkaOffsetStore())
                .AddKafkaClients(
                    clients => clients
                        .WithBootstrapServers("PLAINTEXT://e2e")
                        .AddConsumer(
                            consumer => consumer
                                .WithGroupId(DefaultGroupId)
                                .DisableOffsetsCommit()
                                .StoreOffsetsClientSide(offsetStore => offsetStore.UseMemory())
                                .Consume(endpoint => endpoint.ConsumeFrom(new TopicPartition("topic1", 1)))))
                .AddDelegateSubscriber<TestEventOne>(_ => Interlocked.Increment(ref received))
                .AddIntegrationSpy());

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().First();
        IProducer producer = Helper.GetProducerForEndpoint("topic1[1]");

        for (int i = 0; i < 5; i++)
        {
            await producer.ProduceAsync(new TestEventOne());
        }

        await AsyncTestingUtil.WaitAsync(() => received >= 5);

        received.Should().Be(5);
        Helper.ConsumerGroups.Should().HaveCount(1);
        Helper.ConsumerGroups.First().CommittedOffsets.Should().BeEmpty();

        await consumer.Client.DisconnectAsync();

        // If offsets are properly stored, those will be used to reposition while reconnecting
        await consumer.Client.ConnectAsync();

        for (int i = 0; i < 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne());
        }

        await AsyncTestingUtil.WaitAsync(() => received >= 8);
        received.Should().Be(8);
    }
}
