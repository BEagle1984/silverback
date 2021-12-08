// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaClientConsumerConfigurationBuilderTests
{
    [Fact]
    public void Build_ConfigurationReturned()
    {
        KafkaClientConsumerConfigurationBuilder builder = new();
        builder.WithGroupId("group1");

        KafkaClientConsumerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.GroupId.Should().Be("group1");
    }

    [Fact]
    public void CommitOffsetEach_CommitOffsetEachSet()
    {
        KafkaClientConsumerConfigurationBuilder builder = new();
        builder.CommitOffsetEach(42);

        KafkaClientConsumerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.CommitOffsetEach.Should().Be(42);
    }

    [Fact]
    public void CommitOffsetEach_AutoCommitDisabled()
    {
        KafkaClientConsumerConfigurationBuilder builder = new();
        builder.EnableAutoCommit();
        builder.CommitOffsetEach(42);

        KafkaClientConsumerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.CommitOffsetEach.Should().Be(42);
        configuration.EnableAutoCommit.Should().BeFalse();
    }

    [Fact]
    public void EnableAutoRecovery_EnableAutoRecoverySet()
    {
        KafkaClientConsumerConfigurationBuilder builder = new();
        builder.EnableAutoRecovery();

        KafkaClientConsumerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnableAutoRecovery.Should().BeTrue();
    }

    [Fact]
    public void DisableAutoRecovery_EnableAutoRecoverySet()
    {
        KafkaClientConsumerConfigurationBuilder builder = new();
        builder.DisableAutoRecovery();

        KafkaClientConsumerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnableAutoRecovery.Should().BeFalse();
    }

    [Fact]
    public void EnableAutoCommit_EnableAutoCommitSet()
    {
        KafkaClientConsumerConfigurationBuilder builder = new();
        builder.EnableAutoCommit();

        KafkaClientConsumerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnableAutoCommit.Should().BeTrue();
    }

    [Fact]
    public void EnableAutoCommit_CommitOffsetEachCleared()
    {
        KafkaClientConsumerConfigurationBuilder builder = new();
        builder.CommitOffsetEach(42);
        builder.EnableAutoCommit();

        KafkaClientConsumerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnableAutoCommit.Should().BeTrue();
        configuration.CommitOffsetEach.Should().BeNull();
    }

    [Fact]
    public void DisableAutoCommit_EnableAutoCommitSet()
    {
        KafkaClientConsumerConfigurationBuilder builder = new();
        builder.DisableAutoCommit();

        KafkaClientConsumerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnableAutoCommit.Should().BeFalse();
    }

    [Fact]
    public void EnablePartitionEof_EnablePartitionEofSet()
    {
        KafkaClientConsumerConfigurationBuilder builder = new();
        builder.EnablePartitionEof();

        KafkaClientConsumerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnablePartitionEof.Should().BeTrue();
    }

    [Fact]
    public void DisablePartitionEof_EnablePartitionEofSet()
    {
        KafkaClientConsumerConfigurationBuilder builder = new();
        builder.DisablePartitionEof();

        KafkaClientConsumerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnablePartitionEof.Should().BeFalse();
    }

    [Fact]
    public void AllowAutoCreateTopics_AllowAutoCreateTopicsSet()
    {
        KafkaClientConsumerConfigurationBuilder builder = new();
        builder.AllowAutoCreateTopics();

        KafkaClientConsumerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.AllowAutoCreateTopics.Should().BeTrue();
    }

    [Fact]
    public void DisallowAutoCreateTopics_AllowAutoCreateTopicsSet()
    {
        KafkaClientConsumerConfigurationBuilder builder = new();
        builder.DisallowAutoCreateTopics();

        KafkaClientConsumerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.AllowAutoCreateTopics.Should().BeFalse();
    }
}
