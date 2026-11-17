// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shouldly;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Messages;
using Silverback.Tests.Integration.E2E.TestHost;
using Silverback.Tests.Integration.E2E.TestTypes;
using Silverback.Tests.Integration.E2E.TestTypes.Logging;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka;

public class OffsetStoreInMemoryTests : KafkaTests
{
    private const string MoveTargetTopicName = "other-topic";

    public OffsetStoreInMemoryTests(ITestOutputHelper testOutputHelper)
        : base(testOutputHelper)
    {
    }

    [Fact]
    public async Task OffsetStore_ShouldStoreSubscribedTopicsOffsets()
    {
        int received = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3))
                .AddInMemoryKafkaOffsetStore())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
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

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

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

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        received.ShouldBe(8);
    }

    [Fact]
    public async Task OffsetStore_ShouldStoreOffsets_WhenProcessingIsRetried()
    {
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
                .AddInMemoryKafkaOffsetStore())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .DisableOffsetsCommit()
                    .StoreOffsetsClientSide(offsetStore => offsetStore.UseMemory())
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy.Retry(2)))))
            .AddDelegateSubscriber<TestEventOne>(HandleMessage));

        void HandleMessage(TestEventOne message)
        {
            if (Interlocked.Increment(ref tryCount) == 2)
                throw new InvalidOperationException("Retry!");
        }

        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // The second message fails with the first one already in the store
        tryCount.ShouldBe(3);
        consumer.Client.Status.ShouldBe(ClientStatus.Initialized);

        await consumer.Client.DisconnectAsync();
        await consumer.Client.ConnectAsync();
        await Helper.WaitUntilConnectedAsync();

        await producer.ProduceAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // Both offsets were stored: only the new message is consumed after reconnecting
        tryCount.ShouldBe(4);
    }

    [Fact]
    public async Task OffsetStore_ShouldRetryProcessing_WhenSubscriberFailsAfterStoringOffsets()
    {
        int tryCount = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
                .AddInMemoryKafkaOffsetStore())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .DisableOffsetsCommit()
                    .StoreOffsetsClientSide(offsetStore => offsetStore.UseMemory())
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy.Retry(1)))))
            .AddDelegateSubscriber<TestEventOne, KafkaOffsetStoreScope>(HandleMessageAsync));

        async Task HandleMessageAsync(TestEventOne message, KafkaOffsetStoreScope offsetStoreScope)
        {
            await offsetStoreScope.StoreOffsetsAsync();

            if (Interlocked.Increment(ref tryCount) == 1)
                throw new InvalidOperationException("Retry!");
        }

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.ShouldBe(2);
    }

    [Fact]
    public async Task OffsetStore_ShouldRetryProcessing_WhenCommitOfPreviousMessageFailed()
    {
        int tryCount = 0;
        FailingOffsetCommittedCallback callback = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
                .AddInMemoryKafkaOffsetStore())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .DisableAutoCommit()
                    .CommitOffsetEach(1)
                    .StoreOffsetsClientSide(offsetStore => offsetStore.UseMemory())
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy
                            .Skip(skipPolicy => skipPolicy.ApplyTo<BrokerClientCallbackInvocationException>())
                            .ThenRetry(1)))))
            .AddSingletonBrokerClientCallback(callback)
            .AddDelegateSubscriber<TestEventOne, KafkaOffsetStoreScope>(HandleMessageAsync));

        // The failed commit of the first message is committed again by the skip policy, not rolled back
        async Task HandleMessageAsync(TestEventOne message, KafkaOffsetStoreScope offsetStoreScope)
        {
            if (Interlocked.Increment(ref tryCount) != 2)
                return;

            await offsetStoreScope.StoreOffsetsAsync();

            throw new InvalidOperationException("Retry!");
        }

        // A reconnect would apply the stored offsets as well, without proving anything
        int disconnections = 0;
        IConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().Single();
        consumer.Client.Disconnecting.AddHandler(_ =>
        {
            Interlocked.Increment(ref disconnections);
            return ValueTask.CompletedTask;
        });

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        tryCount.ShouldBe(3);
        disconnections.ShouldBe(0);
    }

    [Fact]
    public async Task OffsetStore_ShouldRetryProcessing_WhenSeekingBackAfterFailedCommit()
    {
        int tryCount = 0;
        FailingOffsetCommittedCallback callback = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
                .AddInMemoryKafkaOffsetStore())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .DisableAutoCommit()
                    .CommitOffsetEach(1)
                    .StoreOffsetsClientSide(offsetStore => offsetStore.UseMemory())
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy
                            .Skip(skipPolicy => skipPolicy.ApplyTo<BrokerClientCallbackInvocationException>())
                            .ThenRetry(1)))))
            .AddSingletonBrokerClientCallback(callback)
            .AddDelegateSubscriber<TestEventOne>(HandleMessage));

        // The first attempt after seeking back fails
        void HandleMessage(TestEventOne message)
        {
            if (Interlocked.Increment(ref tryCount) == 2)
                throw new InvalidOperationException("Retry!");
        }

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().First();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        consumer.Seek(new TopicPartitionOffset(DefaultTopicName, 0, 0));
        await AsyncTestingUtil.WaitAsync(() => tryCount >= 3, TimeSpan.FromSeconds(2));

        tryCount.ShouldBe(3);
    }

    [Fact]
    public async Task OffsetStore_ShouldNotConsumeAgain_WhenCommitFailsAfterStoringOffsets()
    {
        int received = 0;
        int disconnections = 0;
        FailingOffsetCommittedCallback callback = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
                .AddInMemoryKafkaOffsetStore())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .DisableAutoCommit()
                    .CommitOffsetEach(1)
                    .StoreOffsetsClientSide(offsetStore => offsetStore.UseMemory())
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy.Retry(1)))))
            .AddSingletonBrokerClientCallback(callback)
            .AddDelegateSubscriber<TestEventOne>(_ => Interlocked.Increment(ref received)));

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().First();
        consumer.Client.Disconnecting.AddHandler(_ =>
        {
            Interlocked.Increment(ref disconnections);
            return ValueTask.CompletedTask;
        });

        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne());
        await producer.ProduceAsync(new TestEventOne());

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        received.ShouldBe(2);
        disconnections.ShouldBe(0);
    }

    [Fact]
    public async Task OffsetStore_ShouldStoreManuallyAssignedPartitionsOffsets()
    {
        int received = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(3))
                .AddInMemoryKafkaOffsetStore())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
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

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

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

        await Helper.WaitUntilAllMessagesAreConsumedAsync();
        received.ShouldBe(8);
    }

    [Fact]
    public async Task OffsetStore_ShouldStoreOffset_WhenMessageIsSkipped()
    {
        TestingCollection<string> received = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
                .AddInMemoryKafkaOffsetStore())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .DisableOffsetsCommit()
                    .StoreOffsetsClientSide(offsetStore => offsetStore.UseMemory())
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy.Skip()))))
            .AddDelegateSubscriber<TestEventOne>(HandleMessage));

        void HandleMessage(TestEventOne message)
        {
            received.Add(message.ContentEventOne!);

            if (message.ContentEventOne == "2")
                throw new InvalidOperationException("Skip!");
        }

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().First();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // The skipped message is the last one handled on the partition
        GetStoredOffset(consumer).ShouldBe(1);

        await consumer.Client.DisconnectAsync();
        await consumer.Client.ConnectAsync();
        await Helper.WaitUntilConnectedAsync();

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // The skipped message isn't consumed (and skipped) again after reconnecting
        received.ShouldBe(["1", "2", "3"]);

        GetStoredOffset(consumer).ShouldBe(2);
    }

    [Fact]
    public async Task OffsetStore_ShouldStoreOffset_WhenMessageIsMoved()
    {
        TestingCollection<string> received = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
                .AddInMemoryKafkaOffsetStore())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce(endpoint => endpoint.ProduceTo(MoveTargetTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .DisableOffsetsCommit()
                    .StoreOffsetsClientSide(offsetStore => offsetStore.UseMemory())
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy.MoveTo(MoveTargetTopicName)))))
            .AddDelegateSubscriber<TestEventOne>(HandleMessage));

        void HandleMessage(TestEventOne message)
        {
            received.Add(message.ContentEventOne!);

            if (message.ContentEventOne == "2")
                throw new InvalidOperationException("Move!");
        }

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().First();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // The moved message is the last one handled on the partition
        IInMemoryTopic otherTopic = Helper.GetTopic(MoveTargetTopicName);
        otherTopic.MessagesCount.ShouldBe(1);
        GetStoredOffset(consumer).ShouldBe(1);

        await consumer.Client.DisconnectAsync();
        await consumer.Client.ConnectAsync();
        await Helper.WaitUntilConnectedAsync();

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // The moved message isn't consumed (and moved) again after reconnecting
        received.ShouldBe(["1", "2", "3"]);

        otherTopic.MessagesCount.ShouldBe(1);
        GetStoredOffset(consumer).ShouldBe(2);
    }

    [Fact]
    public async Task OffsetStore_ShouldStoreBatchOffsets_WhenBatchIsSkipped()
    {
        TestingCollection<string> received = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
                .AddInMemoryKafkaOffsetStore())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .DisableOffsetsCommit()
                    .StoreOffsetsClientSide(offsetStore => offsetStore.UseMemory())
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(2)
                        .OnError(policy => policy.Skip()))))
            .AddDelegateSubscriber<IEnumerable<TestEventOne>>(HandleBatch));

        void HandleBatch(IEnumerable<TestEventOne> batch)
        {
            List<string> contents = [.. batch.Select(message => message.ContentEventOne!)];

            foreach (string content in contents)
            {
                received.Add(content);
            }

            if (contents.Contains("3"))
                throw new InvalidOperationException("Skip!");
        }

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().First();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        for (int i = 1; i <= 4; i++)
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = i.ToString(CultureInfo.InvariantCulture) });
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // The skipped batch is the last one handled on the partition
        GetStoredOffset(consumer).ShouldBe(3);

        await consumer.Client.DisconnectAsync();
        await consumer.Client.ConnectAsync();
        await Helper.WaitUntilConnectedAsync();

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "5" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "6" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // The skipped batch isn't consumed (and skipped) again after reconnecting
        received.ShouldBe(["1", "2", "3", "4", "5", "6"]);

        GetStoredOffset(consumer).ShouldBe(5);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OffsetStore_ShouldStoreOffset_WhenMessageThatCannotBeDeserializedIsSkippedOrMoved(bool move)
    {
        TestingCollection<string> received = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
                .AddInMemoryKafkaOffsetStore())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddProducer(producer => producer
                    .Produce(endpoint => endpoint.ProduceTo(MoveTargetTopicName)))
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .DisableOffsetsCommit()
                    .StoreOffsetsClientSide(offsetStore => offsetStore.UseMemory())
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(
                            policy =>
                            {
                                if (move)
                                    policy.MoveTo(MoveTargetTopicName);
                                else
                                    policy.Skip();
                            }))))
            .AddDelegateSubscriber<TestEventOne>(HandleMessage));

        void HandleMessage(TestEventOne message) => received.Add(message.ContentEventOne!);

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().First();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.RawProduceAsync(
            "{ invalid"u8.ToArray(),
            [new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)]);

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // The message that cannot be deserialized is the last one handled on the partition
        GetStoredOffset(consumer).ShouldBe(1);

        await consumer.Client.DisconnectAsync();
        await consumer.Client.ConnectAsync();
        await Helper.WaitUntilConnectedAsync();

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // The message isn't consumed (and skipped or moved) again after reconnecting
        received.ShouldBe(["1", "3"]);

        Helper.GetTopic(MoveTargetTopicName).MessagesCount.ShouldBe(move ? 1 : 0);
        GetStoredOffset(consumer).ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OffsetStore_ShouldRetryStoringOffset_WhenStoreFailsOnceForSkippedOrMovedMessage(bool move)
    {
        TestingCollection<string> received = [];
        LogEventsCollector logEventsCollector = new();
        FailingKafkaOffsetStore offsetStore = new(failuresCount: 1);

        await Host.ConfigureServicesAndRunAsync(services =>
        {
            services
                .AddLogging(logging => logging.AddProvider(logEventsCollector))
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options
                    .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
                    .AddInMemoryKafkaOffsetStore())
                .AddKafkaClients(clients => clients
                    .WithBootstrapServers("PLAINTEXT://e2e")
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint.ProduceTo(MoveTargetTopicName)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(DefaultGroupId)
                        .DisableOffsetsCommit()
                        .StoreOffsetsClientSide(offsetStoreSettings => offsetStoreSettings.UseMemory())
                        .Consume(endpoint => endpoint
                            .ConsumeFrom(DefaultTopicName)
                            .OnError(policy => ConfigureSkipOrMovePolicy(policy, move)))))
                .AddDelegateSubscriber<TestEventOne>(HandleMessage);

            OverrideOffsetStore(services, offsetStore);
        });

        void HandleMessage(TestEventOne message)
        {
            received.Add(message.ContentEventOne!);

            if (message.ContentEventOne == "2")
                throw new InvalidOperationException("Skip or move!");
        }

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().First();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // The second attempt stores the offset
        offsetStore.SkippedOffsetsStoreAttempts.ShouldBe(2);
        offsetStore.GetStoredOffsets(DefaultGroupId).Single().Offset.Value.ShouldBe(1);
        logEventsCollector.Count(KafkaLogEvents.SkippedOffsetsStoreError.EventId).ShouldBe(0);

        await consumer.Client.DisconnectAsync();
        await consumer.Client.ConnectAsync();
        await Helper.WaitUntilConnectedAsync();

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // The message isn't consumed (and skipped or moved) again after reconnecting
        received.ShouldBe(["1", "2", "3"]);

        Helper.GetTopic(MoveTargetTopicName).MessagesCount.ShouldBe(move ? 1 : 0);
        offsetStore.GetStoredOffsets(DefaultGroupId).Single().Offset.Value.ShouldBe(2);
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public async Task OffsetStore_ShouldLogErrorAndCommit_WhenStoreFailsForSkippedOrMovedMessage(bool move)
    {
        TestingCollection<string> received = [];
        LogEventsCollector logEventsCollector = new();
        FailingKafkaOffsetStore offsetStore = new(failuresCount: int.MaxValue);

        await Host.ConfigureServicesAndRunAsync(services =>
        {
            services
                .AddLogging(logging => logging.AddProvider(logEventsCollector))
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options
                    .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
                    .AddInMemoryKafkaOffsetStore())
                .AddKafkaClients(clients => clients
                    .WithBootstrapServers("PLAINTEXT://e2e")
                    .AddProducer(producer => producer
                        .Produce(endpoint => endpoint.ProduceTo(MoveTargetTopicName)))
                    .AddConsumer(consumer => consumer
                        .WithGroupId(DefaultGroupId)
                        .DisableAutoCommit()
                        .CommitOffsetEach(1)
                        .StoreOffsetsClientSide(offsetStoreSettings => offsetStoreSettings.UseMemory())
                        .Consume(endpoint => endpoint
                            .ConsumeFrom(DefaultTopicName)
                            .OnError(policy => ConfigureSkipOrMovePolicy(policy, move)))))
                .AddDelegateSubscriber<TestEventOne>(HandleMessage);

            OverrideOffsetStore(services, offsetStore);
        });

        void HandleMessage(TestEventOne message)
        {
            received.Add(message.ContentEventOne!);

            if (message.ContentEventOne == "2")
                throw new InvalidOperationException("Skip or move!");
        }

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().First();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });

        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        // The store is retried once, then the error is logged and the consumer committed anyway
        offsetStore.SkippedOffsetsStoreAttempts.ShouldBe(2);
        offsetStore.GetStoredOffsets(DefaultGroupId).Single().Offset.Value.ShouldBe(0);
        logEventsCollector.Count(KafkaLogEvents.SkippedOffsetsStoreError.EventId).ShouldBe(1);
        logEventsCollector.Contains(KafkaLogEvents.SkippedOffsetsStoreError.EventId, $"{DefaultTopicName}[0]@1").ShouldBeTrue();
        DefaultConsumerGroup.GetCommittedOffsetsCount(DefaultTopicName).ShouldBe(2);
        consumer.Client.Status.ShouldBe(ClientStatus.Initialized);

        // The next message is consumed and its offset stored normally
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });
        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        offsetStore.GetStoredOffsets(DefaultGroupId).Single().Offset.Value.ShouldBe(2);

        await consumer.Client.DisconnectAsync();
        await consumer.Client.ConnectAsync();
        await Helper.WaitUntilConnectedAsync();

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "4" });
        await Helper.WaitUntilAllMessagesAreCommittedAsync();

        // The message was skipped or moved only once
        received.ShouldBe(["1", "2", "3", "4"]);

        Helper.GetTopic(MoveTargetTopicName).MessagesCount.ShouldBe(move ? 1 : 0);
        offsetStore.SkippedOffsetsStoreAttempts.ShouldBe(2);
        logEventsCollector.Count(KafkaLogEvents.SkippedOffsetsStoreError.EventId).ShouldBe(1);
        offsetStore.GetStoredOffsets(DefaultGroupId).Single().Offset.Value.ShouldBe(3);
    }

    [Theory]
    [InlineData("incomplete")]
    [InlineData("duplicated")]
    public async Task OffsetStore_ShouldStoreOffset_WhenChunkIsSkippedWithoutProcessing(string scenario)
    {
        TestEventOne message = new() { ContentEventOne = "chunked" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);
        int received = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
                .AddInMemoryKafkaOffsetStore())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .DisableOffsetsCommit()
                    .StoreOffsetsClientSide(offsetStore => offsetStore.UseMemory())
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddDelegateSubscriber<TestEventOne>(_ => Interlocked.Increment(ref received)));

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().First();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        if (scenario == "incomplete")
        {
            // The chunk of a sequence whose beginning was never consumed
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
            await producer.RawProduceAsync(
                [.. rawMessage.Skip(10).Take(10)],
                HeadersHelper.GetChunkHeaders("1", 1, 3, typeof(TestEventOne)));
        }
        else
        {
            // The chunk consumed again after the sequence was completed
            await producer.RawProduceAsync([.. rawMessage.Take(10)], HeadersHelper.GetChunkHeaders("1", 0, 2, typeof(TestEventOne)));
            await producer.RawProduceAsync([.. rawMessage.Skip(10)], HeadersHelper.GetChunkHeaders("1", 1, 2, typeof(TestEventOne)));
            await producer.RawProduceAsync([.. rawMessage.Skip(10)], HeadersHelper.GetChunkHeaders("1", 1, 2, typeof(TestEventOne)));
        }

        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // The skipped chunk is committed without reaching the subscribers, like it's stored
        received.ShouldBe(1);
        GetStoredOffset(consumer).ShouldBe(scenario == "incomplete" ? 1 : 2);
    }

    [Theory]
    [InlineData("incomplete")]
    [InlineData("duplicated")]
    public async Task OffsetStore_ShouldNotStoreOffsetPastPendingBatch_WhenChunkIsSkippedWithoutProcessing(string scenario)
    {
        TestEventOne message = new() { ContentEventOne = "chunked" };
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(message);
        TestingCollection<List<string?>> completedBatches = [];

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
                .AddInMemoryKafkaOffsetStore())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .DisableOffsetsCommit()
                    .StoreOffsetsClientSide(offsetStore => offsetStore.UseMemory())
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(2))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddIntegrationSpy());

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> contents = [];

            await foreach (TestEventOne batchMessage in batch)
            {
                contents.Add(batchMessage.ContentEventOne);
            }

            completedBatches.Add(contents);
        }

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().First();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        // The chunked message is complete and added to the batch, which is still pending
        await producer.RawProduceAsync([.. rawMessage.Take(10)], HeadersHelper.GetChunkHeaders("1", 0, 2, typeof(TestEventOne)));
        await producer.RawProduceAsync([.. rawMessage.Skip(10)], HeadersHelper.GetChunkHeaders("1", 1, 2, typeof(TestEventOne)));

        if (scenario == "incomplete")
        {
            // The chunk of a sequence whose beginning was never consumed
            await producer.RawProduceAsync(
                [.. rawMessage.Skip(10).Take(5)],
                HeadersHelper.GetChunkHeaders("2", 1, 3, typeof(TestEventOne)));
        }
        else
        {
            // The chunk consumed again after the sequence was completed
            await producer.RawProduceAsync([.. rawMessage.Skip(10)], HeadersHelper.GetChunkHeaders("1", 1, 2, typeof(TestEventOne)));
        }

        // The disconnect awaits the processing of the consumed messages and aborts the pending batch (the skipped chunk isn't committed,
        // so the helper can't wait for it to be consumed)
        await AsyncTestingUtil.WaitAsync(() => Helper.Spy.RawInboundEnvelopes.Count == 3);
        await consumer.Client.DisconnectAsync();

        // The skipped chunk isn't committed, nor its offset stored, since it would skip the chunked message in the pending batch after
        // the restart
        Host.ServiceProvider
            .GetRequiredService<IKafkaOffsetStoreFactory>()
            .GetStore(consumer.Configuration.ClientSideOffsetStore!, Host.ServiceProvider)
            .GetStoredOffsets(DefaultGroupId)
            .ShouldBeEmpty();

        await consumer.Client.ConnectAsync();
        await Helper.WaitUntilConnectedAsync();

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });
        await Helper.WaitUntilAllMessagesAreConsumedAsync();

        // The chunked message is consumed again after reconnecting
        completedBatches.ShouldBe([["chunked", "3"]]);

        GetStoredOffset(consumer).ShouldBe(3);
    }

    [Theory]
    [InlineData("chunked message", false)]
    [InlineData("chunked message", true)]
    [InlineData("message", false)]
    [InlineData("message", true)]
    public async Task OffsetStore_ShouldStoreOffsetOfIncompleteChunkedMessage_WhenAbortedByNextMessage(string nextMessage, bool commitOffsets)
    {
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(new TestEventOne { ContentEventOne = "chunked" });
        TestingCollection<string?> received = [];
        LogEventsCollector logEventsCollector = new();
        RecordingKafkaOffsetStore offsetStore = new();

        await Host.ConfigureServicesAndRunAsync(services =>
        {
            services
                .AddLogging(logging => logging.AddProvider(logEventsCollector))
                .AddSilverback()
                .WithConnectionToMessageBroker(options => options
                    .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
                    .AddInMemoryKafkaOffsetStore())
                .AddKafkaClients(clients => clients
                    .WithBootstrapServers("PLAINTEXT://e2e")
                    .AddConsumer(consumer =>
                    {
                        if (commitOffsets)
                            consumer.DisableAutoCommit().CommitOffsetEach(1);
                        else
                            consumer.DisableOffsetsCommit();

                        consumer
                            .WithGroupId(DefaultGroupId)
                            .StoreOffsetsClientSide(offsetStoreSettings => offsetStoreSettings.UseMemory())
                            .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName));
                    }))
                .AddDelegateSubscriber<TestEventOne>(message => received.Add(message.ContentEventOne));

            OverrideOffsetStore(services, offsetStore);
        });

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().First();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        // The first chunk of a message that is never completed, aborted by the next message (a new chunked message, or a message that
        // doesn't belong to a sequence) within its own pipeline
        await producer.RawProduceAsync([.. rawMessage.Take(5)], HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));

        if (nextMessage == "chunked message")
            await producer.RawProduceAsync([.. rawMessage.Take(10)], HeadersHelper.GetChunkHeaders("2", 0, 2, typeof(TestEventOne)));
        else
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });

        await AsyncTestingUtil.WaitAsync(() => logEventsCollector.Count(IntegrationLogEvents.IncompleteSequenceAborted.EventId) > 0);

        if (nextMessage != "chunked message")
            await AsyncTestingUtil.WaitAsync(() => offsetStore.StoredOffsets.Count == 2);

        // The offset of the aborted message is stored, not the one of the message aborting it (which is stored only once processed)
        offsetStore.StoredOffsets.ShouldBe(nextMessage == "chunked message" ? [0] : [0, 1]);

        // The aborted message is committed like it's stored
        if (commitOffsets)
        {
            await AsyncTestingUtil.WaitAsync(() => GetCommittedOffset() == offsetStore.StoredOffsets[^1] + 1);
            GetCommittedOffset().ShouldBe(offsetStore.StoredOffsets[^1] + 1);
        }

        await consumer.Client.DisconnectAsync();
        await consumer.Client.ConnectAsync();
        await Helper.WaitUntilConnectedAsync();

        if (nextMessage == "chunked message")
        {
            // The pending chunked message is consumed again after reconnecting, and completed by its last chunk
            await producer.RawProduceAsync([.. rawMessage.Skip(10)], HeadersHelper.GetChunkHeaders("2", 1, 2, typeof(TestEventOne)));
            await AsyncTestingUtil.WaitAsync(() => received.Count > 0 && offsetStore.StoredOffsets.Count == 2);

            received.ShouldBe(["chunked"]);
            offsetStore.StoredOffsets.ShouldBe([0, 2]);
        }
        else
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });
            await AsyncTestingUtil.WaitAsync(() => offsetStore.StoredOffsets.Count == 3);

            received.ShouldBe(["1", "2"]);
            offsetStore.StoredOffsets.ShouldBe([0, 1, 2]);
        }

        if (commitOffsets)
        {
            await AsyncTestingUtil.WaitAsync(() => GetCommittedOffset() == 3);
            GetCommittedOffset().ShouldBe(3);
        }
    }

    [Theory]
    [InlineData("timeout", false)]
    [InlineData("timeout", true)]
    [InlineData("next chunked message", false)]
    [InlineData("next chunked message", true)]
    public async Task OffsetStore_ShouldNotMoveStoredOffsetBack_WhenIncompleteChunkedMessageIsAbortedAfterBatchIsCommitted(
        string abortTrigger,
        bool commitOffsets)
    {
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(new TestEventOne { ContentEventOne = "chunked" });
        TestingCollection<List<string?>> batches = [];
        LogEventsCollector logEventsCollector = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging(logging => logging.AddProvider(logEventsCollector))
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
                .AddInMemoryKafkaOffsetStore())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer =>
                {
                    if (commitOffsets)
                        consumer.DisableAutoCommit().CommitOffsetEach(1);
                    else
                        consumer.DisableOffsetsCommit();

                    consumer
                        .WithGroupId(DefaultGroupId)
                        .StoreOffsetsClientSide(offsetStore => offsetStore.UseMemory())
                        .Consume(endpoint => endpoint
                            .ConsumeFrom(DefaultTopicName)
                            .WithSequenceTimeout(TimeSpan.FromSeconds(abortTrigger == "timeout" ? 1 : 600))
                            .EnableBatchProcessing(2));
                }))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync)
            .AddIntegrationSpy());

        async Task HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> contents = [];

            await foreach (TestEventOne batchMessage in batch)
            {
                contents.Add(batchMessage.ContentEventOne);
            }

            batches.Add(contents);
        }

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().First();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        // The first chunk of a message that is never completed, followed by a batch that is stored while the chunked message is pending
        // (the batch doesn't wait for the chunk sequence, which is aborted only by its timeout or by the next chunked message)
        await producer.RawProduceAsync([.. rawMessage.Take(10)], HeadersHelper.GetChunkHeaders("1", 0, 3, typeof(TestEventOne)));
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });
        await AsyncTestingUtil.WaitAsync(() => batches.Count == 1 && GetStoredOffsets(consumer).Count == 1);

        batches.ShouldBe([["1", "2"]]);
        GetStoredOffset(consumer).ShouldBe(2);

        if (abortTrigger != "timeout")
            await producer.RawProduceAsync([.. rawMessage.Take(10)], HeadersHelper.GetChunkHeaders("2", 0, 3, typeof(TestEventOne)));

        await AsyncTestingUtil.WaitAsync(() => logEventsCollector.Count(IntegrationLogEvents.IncompleteSequenceAborted.EventId) > 0);

        // The aborted chunked message is skipped without moving the stored offset (nor the committed one) back before the batch
        logEventsCollector.Count(IntegrationLogEvents.IncompleteSequenceAborted.EventId).ShouldBe(1);
        GetStoredOffset(consumer).ShouldBe(2);

        if (commitOffsets)
            DefaultConsumerGroup.GetCommittedOffset(new TopicPartition(DefaultTopicName, 0))!.Offset.Value.ShouldBe(3);

        await consumer.Client.DisconnectAsync();
        await consumer.Client.ConnectAsync();
        await Helper.WaitUntilConnectedAsync();

        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "4" });
        await AsyncTestingUtil.WaitAsync(() => batches.Count >= 2);
        await AsyncTestingUtil.WaitAsync(() => GetStoredOffset(consumer) >= (abortTrigger == "timeout" ? 4 : 5));

        // The batch isn't consumed again after reconnecting
        batches.ShouldBe([["1", "2"], ["3", "4"]]);
        GetStoredOffset(consumer).ShouldBe(abortTrigger == "timeout" ? 4 : 5);

        if (commitOffsets)
        {
            DefaultConsumerGroup.GetCommittedOffset(new TopicPartition(DefaultTopicName, 0))!.Offset.Value
                .ShouldBe(GetStoredOffset(consumer) + 1);
        }
    }

    [Fact]
    public async Task OffsetStore_ShouldStoreOffsets_WhenPartitionIsRevokedWhileProcessing()
    {
        TestingCollection<string?> received = [];
        TaskCompletionSource firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        LogEventsCollector logEventsCollector = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging(logging => logging.AddProvider(logEventsCollector))
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
                .AddInMemoryKafkaOffsetStore())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .DisableOffsetsCommit()
                    .StoreOffsetsClientSide(offsetStore => offsetStore.UseMemory())
                    .Consume(endpoint => endpoint.ConsumeFrom(DefaultTopicName))))
            .AddDelegateSubscriber<TestEventOne, KafkaOffsetStoreScope>(HandleMessageAsync));

        // The first message stores its offsets only after its partition was revoked
        async Task HandleMessageAsync(TestEventOne message, KafkaOffsetStoreScope offsetStoreScope)
        {
            received.Add(message.ContentEventOne);

            if (received.Count == 1)
            {
                firstStarted.SetResult();
                await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
            }

            await offsetStoreScope.StoreOffsetsAsync();
        }

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().First();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        try
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
            await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

            DefaultConsumerGroup.Rebalance();
            await AsyncTestingUtil.WaitAsync(logEventsCollector.ContainsChannelStopping);
            logEventsCollector.ContainsChannelStopping().ShouldBeTrue();
        }
        finally
        {
            release.TrySetResult();
        }

        await AsyncTestingUtil.WaitAsync(() =>
            logEventsCollector.Contains(KafkaLogEvents.PartitionAssigned.EventId, $"{DefaultTopicName}[0]"));
        await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });
        await AsyncTestingUtil.WaitAsync(() => received.Count >= 2 && GetStoredOffsets(consumer).SingleOrDefault()?.Offset.Value == 1);

        // The message processed while the partition was being revoked is stored atomically with its changes, even if it isn't committed
        // to Kafka: it isn't consumed again after the partition is assigned again
        received.ShouldBe(["1", "2"]);
        GetStoredOffset(consumer).ShouldBe(1);
    }

    [Fact]
    public async Task OffsetStore_ShouldNotStoreOffset_WhenMessageIsSkippedAfterPartitionIsRevoked()
    {
        TestingCollection<string?> received = [];
        TaskCompletionSource firstStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource release = new(TaskCreationOptions.RunContinuationsAsynchronously);
        LogEventsCollector logEventsCollector = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging(logging => logging.AddProvider(logEventsCollector))
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
                .AddInMemoryKafkaOffsetStore())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .DisableOffsetsCommit()
                    .StoreOffsetsClientSide(offsetStore => offsetStore.UseMemory())
                    .Consume(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .OnError(policy => policy.Skip()))))
            .AddDelegateSubscriber<TestEventOne>(HandleMessageAsync));

        // The first message fails (and is skipped) only after its partition was revoked
        async Task HandleMessageAsync(TestEventOne message)
        {
            received.Add(message.ContentEventOne);

            if (received.Count == 1)
            {
                firstStarted.SetResult();
                await release.Task.WaitAsync(TimeSpan.FromSeconds(10));
                throw new InvalidOperationException("Skip!");
            }
        }

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().First();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        try
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
            await firstStarted.Task.WaitAsync(TimeSpan.FromSeconds(10));

            DefaultConsumerGroup.Rebalance();
            await AsyncTestingUtil.WaitAsync(logEventsCollector.ContainsChannelStopping);
            logEventsCollector.ContainsChannelStopping().ShouldBeTrue();
        }
        finally
        {
            release.TrySetResult();
        }

        await AsyncTestingUtil.WaitAsync(() =>
            logEventsCollector.Contains(KafkaLogEvents.PartitionAssigned.EventId, $"{DefaultTopicName}[0]"));
        await AsyncTestingUtil.WaitAsync(() => received.Count >= 2 && GetStoredOffsets(consumer).Count == 1);

        // Like it isn't committed to Kafka, the message skipped after its partition was revoked isn't stored: it's consumed again after
        // the partition is assigned again
        received.ShouldBe(["1", "1"]);
        GetStoredOffset(consumer).ShouldBe(0);
    }

    [Fact]
    public async Task OffsetStore_ShouldNotStoreOffsets_WhenPartitionIsRolledBackWhileProcessing()
    {
        TestingCollection<List<string?>> batches = [];
        TaskCompletionSource firstBatchCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseFirstBatch = new(TaskCreationOptions.RunContinuationsAsynchronously);
        LogEventsCollector logEventsCollector = new();

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging(logging => logging.AddProvider(logEventsCollector))
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
                .AddInMemoryKafkaOffsetStore())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .StoreOffsetsClientSide(offsetStore => offsetStore.UseMemory())
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(10, TimeSpan.FromMilliseconds(500))
                        .OnError(policy => policy.Retry(1)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        // The first batch is completed by its timeout and completes its processing only after the next message started rolling back
        // the partition
        async ValueTask HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];

            try
            {
                await foreach (TestEventOne message in batch)
                {
                    list.Add(message.ContentEventOne);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (batches.Count == 0)
            {
                firstBatchCompleted.TrySetResult();
                await releaseFirstBatch.Task.WaitAsync(TimeSpan.FromSeconds(10));
            }

            batches.Add(list);
        }

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().First();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        try
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
            await firstBatchCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));

            // The next message fails before the sequencer and is rolled back (to be retried) while the first batch is being processed: the
            // rollback marks the partition as being rolled back, then stops its channel, which waits for the first batch
            await producer.RawProduceAsync(
                "{ invalid"u8.ToArray(),
                [new MessageHeader(DefaultMessageHeaders.MessageType, typeof(TestEventOne).AssemblyQualifiedName)]);
            await AsyncTestingUtil.WaitAsync(logEventsCollector.ContainsChannelStopping);
            logEventsCollector.ContainsChannelStopping().ShouldBeTrue();
        }
        finally
        {
            releaseFirstBatch.TrySetResult();
        }

        // The message fails again after the rollback, stopping the consumer
        await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.Status == ConsumerStatus.Stopped);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);

        // Like it isn't committed to Kafka, the offset of the batch completed while the partition was being rolled back isn't stored
        batches.ShouldBe([["1"]]);
        GetStoredOffsets(consumer).ShouldBeEmpty();
        GetCommittedOffset().ShouldBe(-1);
    }

    [Fact]
    [SuppressMessage("ReSharper", "RedundantAssignment", Justification = "Read by the subscriber after the reconnection")]
    public async Task OffsetStore_ShouldNotStoreOffsetsPastFailedBatch_WhenNextMessagesAreConsumedWhileItIsHandled()
    {
        TestingCollection<List<string?>> successfulBatches = [];
        TaskCompletionSource firstBatchCompleted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource releaseFirstBatch = new(TaskCreationOptions.RunContinuationsAsynchronously);
        bool isFailing = true;
        int failures = 0;

        await Host.ConfigureServicesAndRunAsync(services => services
            .AddLogging()
            .AddSilverback()
            .WithConnectionToMessageBroker(options => options
                .AddMockedKafka(mockOptions => mockOptions.WithDefaultPartitionsCount(1))
                .AddInMemoryKafkaOffsetStore())
            .AddKafkaClients(clients => clients
                .WithBootstrapServers("PLAINTEXT://e2e")
                .AddConsumer(consumer => consumer
                    .WithGroupId(DefaultGroupId)
                    .CommitOffsetEach(1)
                    .StoreOffsetsClientSide(offsetStore => offsetStore.UseMemory())
                    .Consume<TestEventOne>(endpoint => endpoint
                        .ConsumeFrom(DefaultTopicName)
                        .EnableBatchProcessing(2, TimeSpan.FromMilliseconds(500))
                        .OnError(policy => policy.Retry(1)))))
            .AddDelegateSubscriber<IAsyncEnumerable<TestEventOne>>(HandleBatchAsync));

        // The batches containing the first message fail (retried once, then stopping the consumer) while isFailing is set. The first one
        // is completed by its timeout and fails only after the next messages were produced.
        async ValueTask HandleBatchAsync(IAsyncEnumerable<TestEventOne> batch)
        {
            List<string?> list = [];

            try
            {
                await foreach (TestEventOne message in batch)
                {
                    list.Add(message.ContentEventOne);
                }
            }
            catch (OperationCanceledException)
            {
                return;
            }

            if (isFailing && list.Contains("1"))
            {
                if (Interlocked.Increment(ref failures) == 1)
                {
                    firstBatchCompleted.TrySetResult();
                    await releaseFirstBatch.Task.WaitAsync(TimeSpan.FromSeconds(10));
                }

                throw new InvalidOperationException("Stop!");
            }

            successfulBatches.Add(list);
        }

        KafkaConsumer consumer = Host.ServiceProvider.GetRequiredService<IConsumerCollection>().OfType<KafkaConsumer>().First();
        IProducer producer = Helper.GetProducerForEndpoint(DefaultTopicName);

        try
        {
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = "1" });
            await firstBatchCompleted.Task.WaitAsync(TimeSpan.FromSeconds(10));

            // The next messages would complete the next batch, but it isn't started while the first one is being processed
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = "2" });
            await producer.ProduceAsync(new TestEventOne { ContentEventOne = "3" });
            await AsyncTestingUtil.WaitAsync(() => successfulBatches.Count > 0, TimeSpan.FromSeconds(1));
            successfulBatches.ShouldBeEmpty();
        }
        finally
        {
            releaseFirstBatch.TrySetResult();
        }

        // The first batch fails, is rolled back and fails again, stopping the consumer
        await AsyncTestingUtil.WaitAsync(() => consumer.StatusInfo.Status == ConsumerStatus.Stopped);
        consumer.StatusInfo.Status.ShouldBe(ConsumerStatus.Stopped);

        // Nothing is stored past the failed batch, like nothing is committed to Kafka
        successfulBatches.ShouldBeEmpty();
        GetStoredOffsets(consumer).ShouldBeEmpty();
        GetCommittedOffset().ShouldBe(-1);

        isFailing = false;
        await consumer.Client.DisconnectAsync();
        await consumer.Client.ConnectAsync();
        await Helper.WaitUntilConnectedAsync();
        await AsyncTestingUtil.WaitAsync(() => GetCommittedOffset() == 3 && GetStoredOffsets(consumer).SingleOrDefault()?.Offset.Value == 2);

        // The failed batch is consumed again after reconnecting
        successfulBatches.SelectMany(batch => batch).ShouldBe(["1", "2", "3"]);
        GetStoredOffset(consumer).ShouldBe(2);
        GetCommittedOffset().ShouldBe(3);
    }

    private static void ConfigureSkipOrMovePolicy(IErrorPolicyBuilder policy, bool move)
    {
        if (move)
            policy.MoveTo(MoveTargetTopicName);
        else
            policy.Skip();
    }

    // Replaces the in-memory offset store, created on demand by the factory
    private static void OverrideOffsetStore(IServiceCollection services, IKafkaOffsetStore offsetStore) =>
        ((KafkaOffsetStoreFactory)services.Single(descriptor => descriptor.ServiceType == typeof(KafkaOffsetStoreFactory)).ImplementationInstance!)
        .OverrideFactories((_, _) => offsetStore);

    private long GetStoredOffset(KafkaConsumer consumer) => GetStoredOffsets(consumer).Single().Offset.Value;

    private long GetCommittedOffset() => DefaultConsumerGroup.GetCommittedOffset(new TopicPartition(DefaultTopicName, 0))?.Offset.Value ?? -1;

    private IReadOnlyCollection<KafkaOffset> GetStoredOffsets(KafkaConsumer consumer) =>
        Host.ServiceProvider
            .GetRequiredService<IKafkaOffsetStoreFactory>()
            .GetStore(consumer.Configuration.ClientSideOffsetStore!, Host.ServiceProvider)
            .GetStoredOffsets(DefaultGroupId);

    // Fails storing the offsets outside a storage transaction (i.e., the offsets of the skipped messages) the specified number of times
    private sealed class FailingKafkaOffsetStore : IKafkaOffsetStore
    {
        private readonly InMemoryKafkaOffsetStore _innerStore = new();

        private readonly int _failuresCount;

        private int _skippedOffsetsStoreAttempts;

        public FailingKafkaOffsetStore(int failuresCount)
        {
            _failuresCount = failuresCount;
        }

        public int SkippedOffsetsStoreAttempts => _skippedOffsetsStoreAttempts;

        public IReadOnlyCollection<KafkaOffset> GetStoredOffsets(string groupId) => _innerStore.GetStoredOffsets(groupId);

        public Task StoreOffsetsAsync(string groupId, IEnumerable<KafkaOffset> offsets, ISilverbackContext? context = null)
        {
            if (context == null && Interlocked.Increment(ref _skippedOffsetsStoreAttempts) <= _failuresCount)
                return Task.FromException(new InvalidOperationException("Store failed."));

            return _innerStore.StoreOffsetsAsync(groupId, offsets, context);
        }
    }

    // Records each stored offset (of the single partition)
    private sealed class RecordingKafkaOffsetStore : IKafkaOffsetStore
    {
        private readonly InMemoryKafkaOffsetStore _innerStore = new();

        public TestingCollection<long> StoredOffsets { get; } = [];

        public IReadOnlyCollection<KafkaOffset> GetStoredOffsets(string groupId) => _innerStore.GetStoredOffsets(groupId);

        public Task StoreOffsetsAsync(string groupId, IEnumerable<KafkaOffset> offsets, ISilverbackContext? context = null)
        {
            List<KafkaOffset> offsetsList = [.. offsets];
            offsetsList.ForEach(offset => StoredOffsets.Add(offset.Offset.Value));
            return _innerStore.StoreOffsetsAsync(groupId, offsetsList, context);
        }
    }

    private sealed class FailingOffsetCommittedCallback : IKafkaOffsetCommittedCallback
    {
        private int _commitCount;

        public void OnOffsetsCommitted(CommittedOffsets offsets, IKafkaConsumer consumer)
        {
            if (Interlocked.Increment(ref _commitCount) == 1)
                throw new InvalidOperationException("Commit failed.");
        }
    }
}
