// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Shouldly;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Testing.Messaging.Broker.Kafka.Mocks;

public class MockedConsumerGroupsCollectionTests
{
    private const string BootstrapServers = "PLAINTEXT://mock";

    private const string OtherBootstrapServers = "PLAINTEXT://other";

    [Fact]
    public async Task TopicCreated_ShouldRebalanceGroupsSubscribingMatchingPatternOnly()
    {
        MockedKafkaOptions options = new()
        {
            DefaultPartitionsCount = 1,
            PartitionsAssignmentDelay = TimeSpan.Zero
        };
        InMemoryTopicCollection topics = new(options);
        MockedConsumerGroupsCollection groups = new(topics);
        MockedConsumerGroup patternGroup = (MockedConsumerGroup)groups.Get("pattern-group", BootstrapServers);
        MockedConsumerGroup nameGroup = (MockedConsumerGroup)groups.Get("name-group", BootstrapServers);
        MockedConsumerGroup otherClusterGroup = (MockedConsumerGroup)groups.Get("pattern-group", OtherBootstrapServers);
        MockedConfluentConsumer patternConsumer = GetConsumer(patternGroup, BootstrapServers, topics, options);
        MockedConfluentConsumer nameConsumer = GetConsumer(nameGroup, BootstrapServers, topics, options);
        MockedConfluentConsumer otherClusterConsumer = GetConsumer(otherClusterGroup, OtherBootstrapServers, topics, options);
        topics.Get("topic-0", BootstrapServers);
        topics.Get("topic-0", OtherBootstrapServers);

        patternConsumer.Subscribe("^topic-");
        nameConsumer.Subscribe("other");
        otherClusterConsumer.Subscribe("^topic-");

        using CancellationTokenSource consumerCancellationTokenSource = new();
        List<Task> consumerTasks =
        [
            StartConsumer(patternConsumer, consumerCancellationTokenSource.Token),
            StartConsumer(nameConsumer, consumerCancellationTokenSource.Token),
            StartConsumer(otherClusterConsumer, consumerCancellationTokenSource.Token)
        ];

        try
        {
            await WaitUntilAllPartitionsAreAssignedAsync(patternGroup);
            await WaitUntilAllPartitionsAreAssignedAsync(nameGroup);
            await WaitUntilAllPartitionsAreAssignedAsync(otherClusterGroup);

            topics.Get("topic-a", BootstrapServers);

            nameGroup.IsRebalanceScheduled.ShouldBeFalse();
            otherClusterGroup.IsRebalanceScheduled.ShouldBeFalse();

            await WaitUntilAllPartitionsAreAssignedAsync(patternGroup);

            patternConsumer.Assignment.ShouldBe([new TopicPartition("topic-0", 0), new TopicPartition("topic-a", 0)], true);
            nameConsumer.Assignment.ShouldBe([new TopicPartition("other", 0)]);
            otherClusterConsumer.Assignment.ShouldBe([new TopicPartition("topic-0", 0)]);
        }
        finally
        {
            await consumerCancellationTokenSource.CancelAsync();
            await Task.WhenAll(consumerTasks);
        }
    }

    [Fact]
    public async Task TopicCreated_ShouldNotRebalanceAgain_WhenTopicSubscribedByNameIsCreatedBySubscription()
    {
        MockedKafkaOptions options = new()
        {
            DefaultPartitionsCount = 1,
            PartitionsAssignmentDelay = TimeSpan.Zero
        };
        InMemoryTopicCollection topics = new(options);
        MockedConsumerGroupsCollection groups = new(topics);
        MockedConsumerGroup group = (MockedConsumerGroup)groups.Get("group", BootstrapServers);
        MockedConfluentConsumer consumer = GetConsumer(group, BootstrapServers, topics, options);
        int assignedCallbacks = 0;
        consumer.PartitionsAssignedHandler = (_, partitions) =>
        {
            Interlocked.Increment(ref assignedCallbacks);
            return partitions.Select(partition => new TopicPartitionOffset(partition, Offset.Unset));
        };

        // The rebalance creates topic-x, which also matches the pattern
        consumer.Subscribe(["topic-x", "^topic-"]);

        using CancellationTokenSource consumerCancellationTokenSource = new();
        Task consumerTask = StartConsumer(consumer, consumerCancellationTokenSource.Token);

        try
        {
            await WaitUntilAllPartitionsAreAssignedAsync(group);

            group.IsRebalanceScheduled.ShouldBeFalse();
            assignedCallbacks.ShouldBe(1);
            consumer.Assignment.ShouldBe([new TopicPartition("topic-x", 0)]);
        }
        finally
        {
            await consumerCancellationTokenSource.CancelAsync();
            await consumerTask;
        }
    }

    [Fact]
    public async Task TopicCreated_ShouldRebalanceGroup_WhenTopicIsCreatedThroughAdminClient()
    {
        MockedKafkaOptions options = new()
        {
            DefaultPartitionsCount = 1,
            PartitionsAssignmentDelay = TimeSpan.Zero
        };
        InMemoryTopicCollection topics = new(options);
        MockedConsumerGroupsCollection groups = new(topics);
        MockedConsumerGroup group = (MockedConsumerGroup)groups.Get("group", BootstrapServers);
        MockedConfluentConsumer consumer = GetConsumer(group, BootstrapServers, topics, options);
        topics.Get("topic-0", BootstrapServers);

        consumer.Subscribe("^topic-");

        using CancellationTokenSource consumerCancellationTokenSource = new();
        Task consumerTask = StartConsumer(consumer, consumerCancellationTokenSource.Token);

        try
        {
            await WaitUntilAllPartitionsAreAssignedAsync(group);

            using MockedConfluentAdminClient adminClient = new(new AdminClientConfig { BootstrapServers = BootstrapServers }, topics);
            await adminClient.CreateTopicsAsync([new TopicSpecification { Name = "topic-a", NumPartitions = 2 }]);
            await WaitUntilAllPartitionsAreAssignedAsync(group);

            consumer.Assignment.ShouldBe(
                [
                    new TopicPartition("topic-0", 0),
                    new TopicPartition("topic-a", 0),
                    new TopicPartition("topic-a", 1)
                ],
                true);
        }
        finally
        {
            await consumerCancellationTokenSource.CancelAsync();
            await consumerTask;
        }
    }

    private static MockedConfluentConsumer GetConsumer(
        MockedConsumerGroup consumerGroup,
        string bootstrapServers,
        IInMemoryTopicCollection topics,
        IMockedKafkaOptions options) =>
        new(
            new ConsumerConfig
            {
                BootstrapServers = bootstrapServers,
                EnableAutoCommit = false,
                GroupId = consumerGroup.GroupId
            },
            topics,
            consumerGroup,
            options);

    private static Task StartConsumer(MockedConfluentConsumer consumer, CancellationToken cancellationToken) =>
        Task.Run(
            () =>
            {
                try
                {
                    consumer.Consume(cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    // Expected when stopping the background consume loop
                }
            },
            CancellationToken.None);

    private static async Task WaitUntilAllPartitionsAreAssignedAsync(MockedConsumerGroup consumerGroup)
    {
        using CancellationTokenSource timeout = new(TimeSpan.FromSeconds(5));
        await consumerGroup.WaitUntilAllMessagesAreConsumedAsync([], timeout.Token);
    }
}
