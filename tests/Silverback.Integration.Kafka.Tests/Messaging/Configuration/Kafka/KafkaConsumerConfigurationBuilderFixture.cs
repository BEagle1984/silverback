// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Confluent.Kafka;
using NSubstitute;
using Shouldly;
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
        KafkaConsumerConfigurationBuilder builder = new(Substitute.For<IServiceProvider>());

        Action act = () => builder.Build();

        act.ShouldThrow<SilverbackConfigurationException>();
    }

    [Fact]
    public void Build_ShouldReturnNewConfiguration_WhenReused()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithGroupId("group1");
        KafkaConsumerConfiguration configuration1 = builder.Build();

        builder.WithGroupId("group2");
        KafkaConsumerConfiguration configuration2 = builder.Build();

        configuration1.GroupId.ShouldBe("group1");
        configuration2.GroupId.ShouldBe("group2");
    }

    [Fact]
    public void Consume_ShouldAddEndpointsWithoutMessageType()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Consume(endpoint => endpoint.ConsumeFrom("topic1"))
            .Consume(endpoint => endpoint.ConsumeFrom("topic2"));

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.Endpoints.Count.ShouldBe(2);
        KafkaConsumerEndpointConfiguration endpoint1 = configuration.Endpoints.First();
        endpoint1.TopicPartitions.Count.ShouldBe(1);
        endpoint1.TopicPartitions.First().ShouldBe(new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset));
        endpoint1.Deserializer.ShouldBeOfType<JsonMessageDeserializer<object>>();
        KafkaConsumerEndpointConfiguration endpoint2 = configuration.Endpoints.Skip(1).First();
        endpoint2.TopicPartitions.Count.ShouldBe(1);
        endpoint2.TopicPartitions.First().ShouldBe(new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset));
        endpoint2.Deserializer.ShouldBeOfType<JsonMessageDeserializer<object>>();
    }

    [Fact]
    public void Consume_ShouldAddEndpointsForMessageType()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom("topic1"))
            .Consume<TestEventTwo>(endpoint => endpoint.ConsumeFrom("topic2"));

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.Endpoints.Count.ShouldBe(2);
        KafkaConsumerEndpointConfiguration endpoint1 = configuration.Endpoints.First();
        endpoint1.TopicPartitions.Count.ShouldBe(1);
        endpoint1.TopicPartitions.First().ShouldBe(new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset));
        endpoint1.Deserializer.ShouldBeOfType<JsonMessageDeserializer<TestEventOne>>();
        KafkaConsumerEndpointConfiguration endpoint2 = configuration.Endpoints.Skip(1).First();
        endpoint2.TopicPartitions.Count.ShouldBe(1);
        endpoint2.TopicPartitions.First().ShouldBe(new TopicPartitionOffset("topic2", Partition.Any, Offset.Unset));
        endpoint2.Deserializer.ShouldBeOfType<JsonMessageDeserializer<TestEventTwo>>();
    }

    [Fact]
    public void Consume_ShouldIgnoreEndpointsWithSameId()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Consume("id1", endpoint => endpoint.ConsumeFrom("topic1"))
            .Consume<TestEventTwo>("id1", endpoint => endpoint.ConsumeFrom("topic2"));

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.Endpoints.Count.ShouldBe(1);
        configuration.Endpoints.First().TopicPartitions.Count.ShouldBe(1);
        configuration.Endpoints.First().TopicPartitions.ShouldBe(
        [
            new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset)
        ]);
        configuration.Endpoints.First().Deserializer.ShouldBeOfType<JsonMessageDeserializer<object>>();
    }

    [Fact]
    public void Consume_ShouldIgnoreEndpointsWithSameTopicNameAndNoId()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Consume<TestEventOne>(endpoint => endpoint.ConsumeFrom("topic1").EnableBatchProcessing(42))
            .Consume<TestEventTwo>(endpoint => endpoint.ConsumeFrom("topic1").EnableBatchProcessing(1000));

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.Endpoints.Count.ShouldBe(1);
        KafkaConsumerEndpointConfiguration endpoint1 = configuration.Endpoints.First();
        endpoint1.TopicPartitions.Count.ShouldBe(1);
        endpoint1.TopicPartitions.First().ShouldBe(new TopicPartitionOffset("topic1", Partition.Any, Offset.Unset));
        endpoint1.Deserializer.ShouldBeOfType<JsonMessageDeserializer<TestEventOne>>();
        endpoint1.Batch!.Size.ShouldBe(42);
    }

    [Fact]
    public void Consume_ShouldAddEndpointsWithSameTopicNameAndDifferentId()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfiguration();

        builder
            .Consume("id1", endpoint => endpoint.ConsumeFrom("topic1"))
            .Consume<TestEventTwo>("id2", endpoint => endpoint.ConsumeFrom("topic1"));

        Action act = () => builder.Build();

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("Cannot connect to the same topic in different endpoints in the same consumer.");
    }

    [Fact]
    public void WithGroupId_ShouldSetGroupId()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithGroupId("group1");

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.GroupId.ShouldBe("group1");
    }

    [Fact]
    public void EnableOffsetsCommit_ShouldSetCommitOffsets()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.EnableOffsetsCommit();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.CommitOffsets.ShouldBe(true);
    }

    [Fact]
    public void DisableOffsetsCommit_ShouldSetCommitOffsets()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.EnableOffsetsCommit();

        builder.DisableOffsetsCommit();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.CommitOffsets.ShouldBe(false);
    }

    [Fact]
    public void DisableOffsetsCommit_ShouldDisableAutoCommit()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.EnableAutoCommit();

        builder.DisableOffsetsCommit();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.CommitOffsets.ShouldBe(false);
        configuration.EnableAutoCommit.ShouldBe(false);
    }

    [Fact]
    public void DisableOffsetsCommit_ShouldSetCommitOffsetEach()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.CommitOffsetEach(42);

        builder.DisableOffsetsCommit();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.CommitOffsets.ShouldBe(false);
        configuration.EnableAutoCommit.ShouldBe(false);
        configuration.CommitOffsetEach.ShouldBeNull();
    }

    [Fact]
    public void CommitOffsetEach_ShouldSetCommitOffsetEach()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.CommitOffsetEach(42);

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.CommitOffsetEach.ShouldBe(42);
    }

    [Fact]
    public void CommitOffsetEach_ShouldDisableAutoCommit()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.EnableAutoCommit();

        builder.CommitOffsetEach(42);

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.CommitOffsetEach.ShouldBe(42);
        configuration.EnableAutoCommit.ShouldBe(false);
    }

    [Fact]
    public void StoreOffsetsClientSide_ShouldSetOffsetStoreSettings()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        InMemoryKafkaOffsetStoreSettings settings = new();
        builder.StoreOffsetsClientSide(settings).Build();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ClientSideOffsetStore.ShouldBe(settings);
    }

    [Fact]
    public void StoreOffsetsClientSide_ShouldSetOffsetStoreSettingsUsingBuilder()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.StoreOffsetsClientSide(offsetStore => offsetStore.UseMemory().WithName("test-store")).Build();

        KafkaConsumerConfiguration configuration = builder.Build();
        InMemoryKafkaOffsetStoreSettings offsetStoreSettings = configuration.ClientSideOffsetStore.ShouldBeOfType<InMemoryKafkaOffsetStoreSettings>();
        offsetStoreSettings.OffsetStoreName.ShouldBe("test-store");
    }

    [Fact]
    public void SendOffsetsToTransaction_ShouldSetSendOffsetsToTransactionAndDisableOffsetsCommit()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.SendOffsetsToTransaction();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.SendOffsetsToTransaction.ShouldBe(true);
        configuration.CommitOffsets.ShouldBe(false);
        configuration.EnableAutoCommit.ShouldBe(false);
        configuration.CommitOffsetEach.ShouldBeNull();
    }

    [Fact]
    public void DisableSendOffsetsToTransaction_ShouldSetSendOffsetsToTransaction()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.SendOffsetsToTransaction();

        builder.DisableSendOffsetsToTransaction();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.SendOffsetsToTransaction.ShouldBe(false);
    }

    [Fact]
    public void EnableAutoRecovery_ShouldSetEnableAutoRecovery()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.EnableAutoRecovery();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.EnableAutoRecovery.ShouldBe(true);
    }

    [Fact]
    public void DisableAutoRecovery_ShouldSetEnableAutoRecovery()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.DisableAutoRecovery();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.EnableAutoRecovery.ShouldBe(false);
    }

    [Fact]
    public void EnableAutoCommit_ShouldSetEnableAutoCommit()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.EnableAutoCommit();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.EnableAutoCommit.ShouldBe(true);
    }

    [Fact]
    public void EnableAutoCommit_ShouldClearCommitOffsetEach()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();
        builder.CommitOffsetEach(42);

        builder.EnableAutoCommit();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.EnableAutoCommit.ShouldBe(true);
        configuration.CommitOffsetEach.ShouldBeNull();
    }

    [Fact]
    public void DisableAutoCommit_ShouldSetEnableAutoCommit()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.DisableAutoCommit();

        Action act = () => builder.Build();

        Exception exception = act.ShouldThrow<SilverbackConfigurationException>();
        exception.Message.ShouldBe("CommitOffsetEach must be greater or equal to 1 when auto-commit is disabled.");
    }

    [Fact]
    public void EnablePartitionEof_ShouldSetEnablePartitionEof()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.EnablePartitionEof();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.EnablePartitionEof.ShouldBe(true);
    }

    [Fact]
    public void DisablePartitionEof_ShouldSetEnablePartitionEof()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.DisablePartitionEof();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.EnablePartitionEof.ShouldBe(false);
    }

    [Fact]
    public void AllowAutoCreateTopics_ShouldSetAllowAutoCreateTopics()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.AllowAutoCreateTopics();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.AllowAutoCreateTopics.ShouldBe(true);
    }

    [Fact]
    public void DisallowAutoCreateTopics_ShouldSetAllowAutoCreateTopics()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.DisallowAutoCreateTopics();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ShouldNotBeNull();
        configuration.AllowAutoCreateTopics.ShouldBe(false);
    }

    [Fact]
    public void ProcessPartitionsIndependently_ShouldSetProcessPartitionsIndependently()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.ProcessPartitionsIndependently();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ProcessPartitionsIndependently.ShouldBe(true);
    }

    [Fact]
    public void ProcessAllPartitionsTogether_ShouldSetProcessPartitionsIndependently()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.ProcessAllPartitionsTogether();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.ProcessPartitionsIndependently.ShouldBe(false);
    }

    [Fact]
    public void LimitParallelism_ShouldSetMaxDegreeOfParallelism()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.LimitParallelism(42);

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.MaxDegreeOfParallelism.ShouldBe(42);
    }

    [Fact]
    public void LimitBackpressure_ShouldSetBackpressureLimit()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.LimitBackpressure(42);

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.BackpressureLimit.ShouldBe(42);
    }

    [Fact]
    public void WithGetMetadataTimeout_ShouldSetTimeout()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithGetMetadataTimeout(TimeSpan.FromSeconds(42));

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.GetMetadataTimeout.ShouldBe(TimeSpan.FromSeconds(42));
    }

    [Fact]
    public void WithPollingTimeout_ShouldSetTimeout()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithPollingTimeout(TimeSpan.FromSeconds(42));

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.PollingTimeout.ShouldBe(TimeSpan.FromSeconds(42));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void WithPollingTimeout_ShouldThrow_WhenTimeoutIsZeroOrNegative(int seconds)
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        Action act = () => builder.WithPollingTimeout(TimeSpan.FromSeconds(seconds));

        act.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WithStallDetectionThreshold_ShouldSetStallDetectionThreshold()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithStallDetectionThreshold(TimeSpan.FromSeconds(42));

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.StallDetectionThreshold.ShouldBe(TimeSpan.FromSeconds(42));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void WithStallDetectionThreshold_ShouldThrow_WhenTimeoutIsZeroOrNegative(int seconds)
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        Action act = () => builder.WithStallDetectionThreshold(TimeSpan.FromSeconds(seconds));

        act.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WithStallDetectionThreshold_ShouldSetStallDetectionThresholdToNull()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithStallDetectionThreshold(null);

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.StallDetectionThreshold.ShouldBeNull();
    }

    [Fact]
    public void WithRangePartitionAssignmentStrategy_ShouldSetPartitionAssignmentStrategy()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithRangePartitionAssignmentStrategy();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.PartitionAssignmentStrategy.ShouldBe(PartitionAssignmentStrategy.Range);
    }

    [Fact]
    public void WithRoundRobinPartitionAssignmentStrategy_ShouldSetPartitionAssignmentStrategy()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithRoundRobinPartitionAssignmentStrategy();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.PartitionAssignmentStrategy.ShouldBe(PartitionAssignmentStrategy.RoundRobin);
    }

    [Fact]
    public void WithCooperativeStickyPartitionAssignmentStrategy_ShouldSetPartitionAssignmentStrategy()
    {
        KafkaConsumerConfigurationBuilder builder = GetBuilderWithValidConfigurationAndEndpoint();

        builder.WithCooperativeStickyPartitionAssignmentStrategy();

        KafkaConsumerConfiguration configuration = builder.Build();
        configuration.PartitionAssignmentStrategy.ShouldBe(PartitionAssignmentStrategy.CooperativeSticky);
    }

    private static KafkaConsumerConfigurationBuilder GetBuilderWithValidConfigurationAndEndpoint() =>
        GetBuilderWithValidConfiguration().Consume(endpoint => endpoint.ConsumeFrom("topic"));

    private static KafkaConsumerConfigurationBuilder GetBuilderWithValidConfiguration() =>
        new KafkaConsumerConfigurationBuilder(Substitute.For<IServiceProvider>())
            .WithBootstrapServers("PLAINTEXT://test")
            .WithGroupId("consumer1");
}
