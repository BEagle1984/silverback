// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Confluent.Kafka;
using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Serialization;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaConsumerConfigurationBuilderFixture
{
    [Fact]
    public void Build_ShouldThrow_WhenConfigurationIsNotValid()
    {
        KafkaConsumerConfigurationBuilder builder = new();

        Action act = () => builder.Build();

        act.Should().Throw<SilverbackConfigurationException>();
    }

    [Fact]
    public void Build_ShouldReturnNewConfiguration_WhenReused()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithGroupId("group1");
        KafkaConsumerConfiguration configuration1 = builder.Build();

        builder.WithGroupId("group2");
        KafkaConsumerConfiguration configuration2 = builder.Build();

        configuration1.GroupId.Should().Be("group1");
        configuration2.GroupId.Should().Be("group2");
    }

    [Fact]
    public void Consume_ShouldAddEndpointsWithoutMessageType()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Consume(endpoint => endpoint.ConsumeFrom("topic1"))
            .Consume(endpoint => endpoint.ConsumeFrom("topic2"));

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.Endpoints.Should().HaveCount(2);
        KafkaConsumerEndpointConfiguration endpoint1 = configuration.Endpoints.First();
        endpoint1.TopicPartitions.Should().HaveCount(1);
        endpoint1.TopicPartitions.First().Should().BeEquivalentTo(new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset));
        endpoint1.Deserializer.Should().BeOfType<JsonMessageDeserializer<object>>();
        KafkaConsumerEndpointConfiguration endpoint2 = configuration.Endpoints.Skip(1).First();
        endpoint2.TopicPartitions.Should().HaveCount(1);
        endpoint2.TopicPartitions.First().Should().BeEquivalentTo(new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset));
        endpoint2.Deserializer.Should().BeOfType<JsonMessageDeserializer<object>>();
    }

    [Fact]
    public void Consume_ShouldAddEndpointsForMessageType()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom("topic1"))
            .Consume<TestEventTwo>(endpoint => endpoint.ConsumeFrom("topic2"));

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.Endpoints.Should().HaveCount(2);
        KafkaConsumerEndpointConfiguration endpoint1 = configuration.Endpoints.First();
        endpoint1.TopicPartitions.Should().HaveCount(1);
        endpoint1.TopicPartitions.First().Should().BeEquivalentTo(new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset));
        endpoint1.Deserializer.Should().BeOfType<JsonMessageDeserializer<TestEventOne>>();
        KafkaConsumerEndpointConfiguration endpoint2 = configuration.Endpoints.Skip(1).First();
        endpoint2.TopicPartitions.Should().HaveCount(1);
        endpoint2.TopicPartitions.First().Should().BeEquivalentTo(new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset));
        endpoint2.Deserializer.Should().BeOfType<JsonMessageDeserializer<TestEventTwo>>();
    }

    [Fact]
    public void Consume_ShouldIgnoreEndpointsWithSameId()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Consume("id1", endpoint => endpoint.ConsumeFrom("topic1"))
            .Consume<TestEventTwo>("id1", endpoint => endpoint.ConsumeFrom("topic2"));

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.Endpoints.Should().HaveCount(1);
        configuration.Endpoints.First().TopicPartitions.Should().HaveCount(1);
        configuration.Endpoints.First().TopicPartitions.Should().BeEquivalentTo(
            new[]
            {
                new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset)
            });
        configuration.Endpoints.First().Deserializer.Should().BeOfType<JsonMessageDeserializer<object>>();
    }

    [Fact]
    public void Consume_ShouldIgnoreEndpointsWithSameTopicNameAndNoId()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom("topic1").EnableBatchProcessing(42))
            .Consume<TestEventTwo>(endpoint => endpoint.ConsumeFrom("topic1").EnableBatchProcessing(1000));

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.Endpoints.Should().HaveCount(1);
        KafkaConsumerEndpointConfiguration endpoint1 = configuration.Endpoints.First();
        endpoint1.TopicPartitions.Should().HaveCount(1);
        endpoint1.TopicPartitions.First().Should().BeEquivalentTo(new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset));
        endpoint1.Deserializer.Should().BeOfType<JsonMessageDeserializer<TestEventOne>>();
        endpoint1.Batch!.Size.Should().Be(42);
    }

    [Fact]
    public void Consume_ShouldAddEndpointsWithSameTopicNameAndDifferentId()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Consume("id1", endpoint => endpoint.ConsumeFrom("topic1"))
            .Consume<TestEventTwo>("id2", endpoint => endpoint.ConsumeFrom("topic1"));

        Action act = () => builder.Build();

        act.Should().Throw<SilverbackConfigurationException>()
            .WithMessage("Cannot connect to the same topic in different endpoints in the same consumer.");
    }

    [Fact]
    public void WithGroupId_ShouldSetGroupId()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithGroupId("group1");

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.GroupId.Should().Be("group1");
    }

    [InlineData(null)]
    [InlineData("")]
    [Theory]
    public void WithGroupId_ShouldSetGroupIdToUnset_WhenNullOrEmpty(string? groupId)
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfiguration();
        builder.Consume(endpoint => endpoint.ConsumeFrom("topic1", 1));
        builder.DisableOffsetsCommit();

        builder.WithGroupId(groupId);

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.GroupId.Should().Be(KafkaConsumerConfiguration.UnsetGroupId);
    }

    [Fact]
    public void EnableOffsetsCommit_ShouldSetCommitOffsets()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.EnableOffsetsCommit();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.CommitOffsets.Should().BeTrue();
    }

    [Fact]
    public void DisableOffsetsCommit_ShouldSetCommitOffsets()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.EnableOffsetsCommit();

        builder.DisableOffsetsCommit();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.CommitOffsets.Should().BeFalse();
    }

    [Fact]
    public void DisableOffsetsCommit_ShouldDisableAutoCommit()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.EnableAutoCommit();

        builder.DisableOffsetsCommit();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.CommitOffsets.Should().BeFalse();
        configuration.EnableAutoCommit.Should().BeFalse();
    }

    [Fact]
    public void DisableOffsetsCommit_ShouldSetCommitOffsetEach()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.CommitOffsetEach(42);

        builder.DisableOffsetsCommit();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.CommitOffsets.Should().BeFalse();
        configuration.EnableAutoCommit.Should().BeFalse();
        configuration.CommitOffsetEach.Should().BeNull();
    }

    [Fact]
    public void CommitOffsetEach_ShouldSetCommitOffsetEach()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.CommitOffsetEach(42);

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.CommitOffsetEach.Should().Be(42);
    }

    [Fact]
    public void CommitOffsetEach_ShouldDisableAutoCommit()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.EnableAutoCommit();

        builder.CommitOffsetEach(42);

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.CommitOffsetEach.Should().Be(42);
        configuration.EnableAutoCommit.Should().BeFalse();
    }

    [Fact]
    public void StoreOffsetsClientSide_ShouldSetOffsetStoreSettings()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        InMemoryKafkaOffsetStoreSettings settings = new();
        builder.StoreOffsetsClientSide(settings).Build();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ClientSideOffsetStore.Should().Be(settings);
    }

    [Fact]
    public void StoreOffsetsClientSide_ShouldSetOffsetStoreSettingsUsingBuilder()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.StoreOffsetsClientSide(offsetStore => offsetStore.UseMemory().WithName("test-store")).Build();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ClientSideOffsetStore.Should().BeOfType<InMemoryKafkaOffsetStoreSettings>();
        configuration.ClientSideOffsetStore.As<InMemoryKafkaOffsetStoreSettings>().OffsetStoreName.Should().Be("test-store");
    }

    [Fact]
    public void EnableAutoRecovery_ShouldSetEnableAutoRecovery()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.EnableAutoRecovery();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.EnableAutoRecovery.Should().BeTrue();
    }

    [Fact]
    public void DisableAutoRecovery_ShouldSetEnableAutoRecovery()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.DisableAutoRecovery();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.EnableAutoRecovery.Should().BeFalse();
    }

    [Fact]
    public void EnableAutoCommit_ShouldSetEnableAutoCommit()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.EnableAutoCommit();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.EnableAutoCommit.Should().BeTrue();
    }

    [Fact]
    public void EnableAutoCommit_ShouldClearCommitOffsetEach()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.CommitOffsetEach(42);

        builder.EnableAutoCommit();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.EnableAutoCommit.Should().BeTrue();
        configuration.CommitOffsetEach.Should().BeNull();
    }

    [Fact]
    public void DisableAutoCommit_ShouldSetEnableAutoCommit()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.DisableAutoCommit();

        Action act = () => builder.Build();

        act.Should().Throw<SilverbackConfigurationException>()
            .WithMessage("CommitOffsetEach must be greater or equal to 1 when auto-commit is disabled.");
    }

    [Fact]
    public void EnablePartitionEof_ShouldSetEnablePartitionEof()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.EnablePartitionEof();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.EnablePartitionEof.Should().BeTrue();
    }

    [Fact]
    public void DisablePartitionEof_ShouldSetEnablePartitionEof()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.DisablePartitionEof();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.EnablePartitionEof.Should().BeFalse();
    }

    [Fact]
    public void AllowAutoCreateTopics_ShouldSetAllowAutoCreateTopics()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.AllowAutoCreateTopics();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.AllowAutoCreateTopics.Should().BeTrue();
    }

    [Fact]
    public void DisallowAutoCreateTopics_ShouldSetAllowAutoCreateTopics()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.DisallowAutoCreateTopics();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.Should().NotBeNull();
        configuration.AllowAutoCreateTopics.Should().BeFalse();
    }

    [Fact]
    public void ProcessPartitionsIndependently_ShouldSetProcessPartitionsIndependently()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.ProcessPartitionsIndependently();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ProcessPartitionsIndependently.Should().BeTrue();
    }

    [Fact]
    public void ProcessAllPartitionsTogether_ShouldSetProcessPartitionsIndependently()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.ProcessAllPartitionsTogether();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ProcessPartitionsIndependently.Should().BeFalse();
    }

    [Fact]
    public void LimitParallelism_ShouldSetMaxDegreeOfParallelism()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.LimitParallelism(42);

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.MaxDegreeOfParallelism.Should().Be(42);
    }

    [Fact]
    public void LimitBackpressure_ShouldSetBackpressureLimit()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.LimitBackpressure(42);

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.BackpressureLimit.Should().Be(42);
    }

    private static KafkaConsumerConfigurationBuilder GetBuilderWithValidConfigurationAndEndpoint() =>
        GetBuilderWithValidConfiguration().Consume(endpoint => endpoint.ConsumeFrom("topic"));

    private static KafkaConsumerConfigurationBuilder GetBuilderWithValidConfiguration() =>
        new KafkaConsumerConfigurationBuilder()
            .WithBootstrapServers("PLAINTEXT://test")
            .WithGroupId("consumer1");
}
