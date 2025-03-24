// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Storage;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestHost.Database;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Util;
using Xunit;
using Xunit.Abstractions;

namespace Silverback.Tests.Integration.E2E.Kafka;

[SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Test code")]
[Trait("Dependency", "Docker")]
[Trait("Database", "PostgreSql")]
public class OffsetStorePostgreSqlFixture : KafkaFixture
{
    public OffsetStorePostgreSqlFixture(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task OffsetStore_ShouldStoreSubscribedTopicsOffsets()
    {
        using PostgreSqlDatabase database = await PostgreSqlDatabase.StartAsync();

        int received = 0;

        await Host.ConfigureServicesAndRunAsync(
                services => services
                    .AddLogging()
                    .InitDatabase(storageInitializer => storageInitializer.CreatePostgreSqlKafkaOffsetStoreAsync(database.ConnectionString))
                    .AddSilverback()
                        .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3))
                            .AddPostgreSqlKafkaOffsetStore())
                    .AddKafkaClients(
                        clients => clients
                            .WithBootstrapServers("PLAINTEXT://e2e")
                            .AddConsumer(
                                consumer => consumer
                                    .WithGroupId(DefaultGroupId)
                                    .DisableOffsetsCommit()
                                    .StoreOffsetsClientSide(offsetStore => offsetStore.UsePostgreSql(database.ConnectionString))
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

        received.ShouldBe(5);
        Helper.ConsumerGroups.Count.ShouldBe(1);
        Helper.ConsumerGroups.First().CommittedOffsets.ShouldBeEmpty();

        await consumer.Client.DisconnectAsync();

        // If offsets are properly stored, those will be used to reposition while reconnecting
        await consumer.Client.ConnectAsync();

        for (int i = 0; i < 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne());
        }

        await AsyncTestingUtil.WaitAsync(() => received >= 8);
        received.ShouldBe(8);
    }

    [Fact]
    public async Task OffsetStore_ShouldStoreManuallyAssignedPartitionsOffsets()
    {
        using PostgreSqlDatabase database = await PostgreSqlDatabase.StartAsync();

        int received = 0;

        await Host.ConfigureServicesAndRunAsync(
                services => services
                    .AddLogging()
                    .InitDatabase(storageInitializer => storageInitializer.CreatePostgreSqlKafkaOffsetStoreAsync(database.ConnectionString))
                    .AddSilverback()
                        .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3))
                            .AddPostgreSqlKafkaOffsetStore())
                    .AddKafkaClients(
                        clients => clients
                            .WithBootstrapServers("PLAINTEXT://e2e")
                            .AddConsumer(
                                consumer => consumer
                                    .WithGroupId(DefaultGroupId)
                                    .DisableOffsetsCommit()
                                    .StoreOffsetsClientSide(offsetStore => offsetStore.UsePostgreSql(database.ConnectionString))
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

        received.ShouldBe(5);
        Helper.ConsumerGroups.Count.ShouldBe(1);
        Helper.ConsumerGroups.First().CommittedOffsets.ShouldBeEmpty();

        await consumer.Client.DisconnectAsync();

        // If offsets are properly stored, those will be used to reposition while reconnecting
        await consumer.Client.ConnectAsync();

        for (int i = 0; i < 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne());
        }

        await AsyncTestingUtil.WaitAsync(() => received >= 8);
        received.ShouldBe(8);
    }

    [Fact]
    [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "Reviewed")]
    [SuppressMessage("ReSharper", "RedundantAssignment", Justification = "False positive")]
    public async Task OffsetStore_ShouldUseTransaction()
    {
        using PostgreSqlDatabase database = await PostgreSqlDatabase.StartAsync();

        int received = 0;
        bool mustCommit = false;

        await Host.ConfigureServicesAndRunAsync(
                services => services
                    .AddLogging()
                    .InitDatabase(storageInitializer => storageInitializer.CreatePostgreSqlKafkaOffsetStoreAsync(database.ConnectionString))
                    .AddSilverback()
                        .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1)) // TODO: Increase
                            .AddPostgreSqlKafkaOffsetStore())
                    .AddKafkaClients(
                        clients => clients
                            .WithBootstrapServers("PLAINTEXT://e2e")
                            .AddConsumer(
                                consumer => consumer
                                    .WithGroupId(DefaultGroupId)
                                    .DisableOffsetsCommit()
                                    .StoreOffsetsClientSide(offsetStore => offsetStore.UsePostgreSql(database.ConnectionString))
                                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
                    .AddDelegateSubscriber<TestEventOne, KafkaOffsetStoreScope>(HandleAsync)
                    .AddIntegrationSpy());

        async Task HandleAsync(TestEventOne message, KafkaOffsetStoreScope offsetStoreScope)
        {
            await using NpgsqlConnection connection = new(database.ConnectionString);
            await connection.OpenAsync();

            await using (DbTransaction transaction = await connection.BeginTransactionAsync())
            {
                offsetStoreScope.EnlistTransaction(transaction);
                Interlocked.Increment(ref received);

                await offsetStoreScope.StoreOffsetsAsync();

                if (mustCommit)
                    await transaction.CommitAsync();
                else
                    await transaction.RollbackAsync();
            }

            await connection.CloseAsync();
        }

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().First();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 0; i < 5; i++)
        {
            await producer.ProduceAsync(new TestEventOne());
        }

        await AsyncTestingUtil.WaitAsync(() => received >= 5);

        received.ShouldBe(5);
        Helper.ConsumerGroups.Count.ShouldBe(1);
        Helper.ConsumerGroups.First().CommittedOffsets.ShouldBeEmpty();

        await consumer.Client.DisconnectAsync();

        // The client should reconsume the same messages
        mustCommit = true;
        await consumer.Client.ConnectAsync();

        await AsyncTestingUtil.WaitAsync(() => received >= 10);
        received.ShouldBe(10);

        await consumer.Client.DisconnectAsync();

        // If offsets are properly stored, those will be used to reposition while reconnecting
        await consumer.Client.ConnectAsync();

        for (int i = 0; i < 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne());
        }

        await AsyncTestingUtil.WaitAsync(() => received >= 13);
        received.ShouldBe(13);
    }

    [Fact]
    public async Task OffsetStore_ShouldStoreBatchOffsets()
    {
        using PostgreSqlDatabase database = await PostgreSqlDatabase.StartAsync();

        int received = 0;

        await Host.ConfigureServicesAndRunAsync(
                services => services
                    .AddLogging()
                    .InitDatabase(storageInitializer => storageInitializer.CreatePostgreSqlKafkaOffsetStoreAsync(database.ConnectionString))
                    .AddSilverback()
                        .WithConnectionToMessageBroker(
                        options => options
                            .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
                            .AddPostgreSqlKafkaOffsetStore())
                    .AddKafkaClients(
                        clients => clients
                            .WithBootstrapServers("PLAINTEXT://e2e")
                            .AddConsumer(
                                consumer => consumer
                                    .WithGroupId(DefaultGroupId)
                                    .DisableOffsetsCommit()
                                    .StoreOffsetsClientSide(offsetStore => offsetStore.UsePostgreSql(database.ConnectionString))
                                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName).EnableBatchProcessing(5))))
                    .AddDelegateSubscriber<IEnumerable<TestEventOne>>(
                        batch =>
                            batch.ForEach(_ => Interlocked.Increment(ref received)))
                    .AddIntegrationSpy());

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().First();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 0; i < 5; i++)
        {
            await producer.ProduceAsync(new TestEventOne());
        }

        await AsyncTestingUtil.WaitAsync(() => received >= 5);

        received.ShouldBe(5);
        Helper.ConsumerGroups.Count.ShouldBe(1);
        Helper.ConsumerGroups.First().CommittedOffsets.ShouldBeEmpty();

        await consumer.Client.DisconnectAsync();

        // If offsets are properly stored, those will be used to reposition while reconnecting
        await consumer.Client.ConnectAsync();

        for (int i = 0; i < 3; i++)
        {
            await producer.ProduceAsync(new TestEventOne());
        }

        await AsyncTestingUtil.WaitAsync(() => received >= 8);
        received.ShouldBe(8);
    }
}
