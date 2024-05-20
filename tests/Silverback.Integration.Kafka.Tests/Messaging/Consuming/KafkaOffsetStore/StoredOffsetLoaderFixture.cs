// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Confluent.Kafka;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Consuming.KafkaOffsetStore;

public class StoredOffsetLoaderFixture
{
    private readonly KafkaOffsetStoreFactory _storeFactory = new();

    private readonly KafkaConsumerConfiguration _consumerConfiguration = new()
    {
        GroupId = "test-group",
        ClientSideOffsetStore = new InMemoryKafkaOffsetStoreSettings()
    };

    private readonly IServiceProvider _serviceProvider = Substitute.For<IServiceProvider>();

    public StoredOffsetLoaderFixture()
    {
        _storeFactory.AddFactory<InMemoryKafkaOffsetStoreSettings>((_, _) => new InMemoryKafkaOffsetStore());
    }

    [Theory]
    [InlineData(10, 5)]
    [InlineData(5, 5)]
    public async Task ApplyStoredOffset_ShouldReturnStoredOffset_WhenGreaterOrEqualThanCurrent(int storedOffset, int currentOffset)
    {
        await _storeFactory.GetStore(_consumerConfiguration.ClientSideOffsetStore!, _serviceProvider)
            .StoreOffsetsAsync(
                "test-group",
                [
                    new KafkaOffset("test-topic", 1, -1),
                    new KafkaOffset("test-topic", 2, -1),
                    new KafkaOffset("test-topic", 42, storedOffset)
                ]);

        StoredOffsetsLoader loader = new(_storeFactory, _consumerConfiguration, _serviceProvider);

        TopicPartitionOffset result = loader.ApplyStoredOffset(new TopicPartitionOffset("test-topic", 42, currentOffset));

        result.Offset.Value.Should().Be(storedOffset + 1);
        result.Topic.Should().Be("test-topic");
        result.Partition.Value.Should().Be(42);
    }

    [Fact]
    public async Task ApplyStoredOffset_ShouldReturnCurrentOffset_WhenStoredOffsetIsLessThanCurrent()
    {
        await _storeFactory.GetStore(_consumerConfiguration.ClientSideOffsetStore!, _serviceProvider)
            .StoreOffsetsAsync(
                "test-group",
                [
                    new KafkaOffset("test-topic", 1, -1),
                    new KafkaOffset("test-topic", 2, -1),
                    new KafkaOffset("test-topic", 42, 5)
                ]);

        StoredOffsetsLoader loader = new(_storeFactory, _consumerConfiguration, _serviceProvider);

        TopicPartitionOffset result = loader.ApplyStoredOffset(new TopicPartitionOffset("test-topic", 42, 10));

        result.Offset.Value.Should().Be(10);
        result.Topic.Should().Be("test-topic");
        result.Partition.Value.Should().Be(42);
    }

    [Fact]
    public void ApplyStoredOffset_ShouldReturnCurrentOffset_WhenNoStoredOffset()
    {
        StoredOffsetsLoader loader = new(_storeFactory, _consumerConfiguration, _serviceProvider);

        TopicPartitionOffset result = loader.ApplyStoredOffset(new TopicPartitionOffset("test-topic", 42, 5));

        result.Offset.Value.Should().Be(5);
        result.Topic.Should().Be("test-topic");
        result.Partition.Value.Should().Be(42);
    }
}
