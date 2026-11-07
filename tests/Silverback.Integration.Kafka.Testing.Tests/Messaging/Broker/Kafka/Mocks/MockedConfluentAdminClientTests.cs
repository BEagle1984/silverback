// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Confluent.Kafka;
using Confluent.Kafka.Admin;
using Shouldly;
using Silverback.Messaging.Broker.Kafka.Mocks;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Testing.Messaging.Broker.Kafka.Mocks;

public sealed class MockedConfluentAdminClientTests : IDisposable
{
    private const string BootstrapServers = "PLAINTEXT://test:9092";

    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(1);

    private readonly MockedKafkaOptions _options = new();

    private readonly InMemoryTopicCollection _topics;

    private readonly MockedConfluentAdminClient _client;

    public MockedConfluentAdminClientTests()
    {
        _topics = new InMemoryTopicCollection(_options);
        _client = new MockedConfluentAdminClient(new ClientConfig { BootstrapServers = BootstrapServers }, _topics);
    }

    [Fact]
    public async Task CreateTopicsAsync_ShouldCreateTopicsWithSpecifiedPartitions()
    {
        await _client.CreateTopicsAsync(
        [
            new TopicSpecification
            {
                Name = "topic1",
                NumPartitions = 2
            },
            new TopicSpecification
            {
                Name = "topic2",
                NumPartitions = 3
            }
        ]);

        _topics.Get("topic1", BootstrapServers).Partitions.Count.ShouldBe(2);
        _topics.Get("topic2", BootstrapServers).Partitions.Count.ShouldBe(3);
    }

    [Fact]
    public async Task CreateTopicsAsync_ShouldUseConfiguredPartitions_WhenPartitionsNotSpecified()
    {
        _options.TopicPartitionsCount["topic"] = 7;

        await _client.CreateTopicsAsync([new TopicSpecification { Name = "topic" }]);

        _topics.Get("topic", BootstrapServers).Partitions.Count.ShouldBe(7);
    }

    [Fact]
    public async Task CreateTopicsAsync_ShouldUseReplicasAssignments_WhenSpecified()
    {
        await _client.CreateTopicsAsync(
        [
            new TopicSpecification
            {
                Name = "topic",
                ReplicasAssignments = new Dictionary<int, List<int>>
                {
                    { 0, [0] },
                    { 1, [0] },
                    { 2, [0] }
                }
            }
        ]);

        _topics.Get("topic", BootstrapServers).Partitions.Count.ShouldBe(3);
    }

    [Fact]
    public async Task CreateTopicsAsync_ShouldThrow_WhenValidateOnlyIsSet()
    {
        Func<Task> act = () => _client.CreateTopicsAsync(
            [new TopicSpecification { Name = "topic" }],
            new CreateTopicsOptions { ValidateOnly = true });

        await act.ShouldThrowAsync<NotSupportedException>();
        _topics.Count.ShouldBe(0);
    }

    [Fact]
    public async Task CreateTopicsAsync_ShouldThrowAndCreateOtherTopics_WhenTopicAlreadyExists()
    {
        _topics.TryCreate("existing", BootstrapServers, 2);

        Func<Task> act = () => _client.CreateTopicsAsync(
        [
            new TopicSpecification
            {
                Name = "existing",
                NumPartitions = 3
            },
            new TopicSpecification
            {
                Name = "new",
                NumPartitions = 3
            }
        ]);

        CreateTopicsException exception = await act.ShouldThrowAsync<CreateTopicsException>();
        exception.Results.Single(report => report.Topic == "existing").Error.Code.ShouldBe(ErrorCode.TopicAlreadyExists);
        exception.Results.Single(report => report.Topic == "new").Error.Code.ShouldBe(ErrorCode.NoError);
        _topics.Get("existing", BootstrapServers).Partitions.Count.ShouldBe(2);
        _topics.Get("new", BootstrapServers).Partitions.Count.ShouldBe(3);
    }

    [Fact]
    public async Task GetMetadata_ShouldReturnPartitionsOfCreatedTopic()
    {
        await _client.CreateTopicsAsync(
        [
            new TopicSpecification
            {
                Name = "topic",
                NumPartitions = 2
            }
        ]);

        TopicMetadata metadata = _client.GetMetadata("topic", Timeout).Topics.ShouldHaveSingleItem();

        metadata.Topic.ShouldBe("topic");
        metadata.Error.Code.ShouldBe(ErrorCode.NoError);
        metadata.Partitions.Select(partition => partition.PartitionId).ShouldBe([0, 1]);
    }

    [Fact]
    public void GetMetadata_ShouldReturnConfiguredPartitions_WhenTopicNotCreated()
    {
        _options.TopicPartitionsCount["topic"] = 7;

        TopicMetadata metadata = _client.GetMetadata("topic", Timeout).Topics.ShouldHaveSingleItem();

        metadata.Partitions.Count.ShouldBe(7);
    }

    [Fact]
    public void GetMetadata_ShouldReturnAllTopicsOfSameBroker_WhenTopicNotSpecified()
    {
        _topics.TryCreate("topic1", BootstrapServers);
        _topics.TryCreate("topic2", "plaintext://TEST:9092");
        _topics.TryCreate("other", "PLAINTEXT://other:9092");

        Metadata metadata = _client.GetMetadata(Timeout);

        metadata.Topics.Select(topic => topic.Topic).ShouldBe(["topic1", "topic2"], ignoreOrder: true);
    }

    public void Dispose() => _client.Dispose();
}
