// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Kafka.Messaging.Configuration.Kafka;

public class KafkaClientConsumerConfigurationBuilderFixture
{
    [Fact]
    public void Constructor_ShouldCloneBaseConfiguration()
    {
        KafkaClientConfiguration baseConfiguration = new()
        {
            BootstrapServers = "base",
            ClientId = "client"
        };

        KafkaClientConsumerConfigurationBuilder builder1 = new(baseConfiguration);
        builder1.WithBootstrapServers("builder1");
        KafkaClientConsumerConfigurationBuilder builder2 = new(baseConfiguration);
        builder2.WithBootstrapServers("builder2");

        KafkaClientConsumerConfiguration configuration1 = builder1.Build();
        KafkaClientConsumerConfiguration configuration2 = builder2.Build();

        baseConfiguration.BootstrapServers.Should().Be("base");
        configuration1.BootstrapServers.Should().Be("builder1");
        configuration2.BootstrapServers.Should().Be("builder2");

        baseConfiguration.ClientId.Should().Be("client");
        configuration1.ClientId.Should().Be("client");
        configuration2.ClientId.Should().Be("client");
    }

    [Fact]
    public void Build_ShouldReturnConfiguration()
    {
        KafkaClientConsumerConfigurationBuilder builder = new();
        builder.WithGroupId("group1");

        KafkaClientConsumerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.GroupId.Should().Be("group1");
    }

    [Fact]
    public void Build_ShouldReturnNewConfiguration_WhenReused()
    {
        KafkaClientConsumerConfigurationBuilder builder = new();

        builder.WithGroupId("group1");
        KafkaClientConsumerConfiguration configuration1 = builder.Build();

        builder.WithGroupId("group2");
        KafkaClientConsumerConfiguration configuration2 = builder.Build();

        configuration1.GroupId.Should().Be("group1");
        configuration2.GroupId.Should().Be("group2");
    }

    [Fact]
    public void CommitOffsetEach_ShouldSetCommitOffsetEach()
    {
        KafkaClientConsumerConfigurationBuilder builder = new();
        builder.CommitOffsetEach(42);

        KafkaClientConsumerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.CommitOffsetEach.Should().Be(42);
    }

    [Fact]
    public void CommitOffsetEach_ShouldDisableAutoCommit()
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
    public void EnableAutoRecovery_ShouldSetEnableAutoRecovery()
    {
        KafkaClientConsumerConfigurationBuilder builder = new();
        builder.EnableAutoRecovery();

        KafkaClientConsumerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnableAutoRecovery.Should().BeTrue();
    }

    [Fact]
    public void DisableAutoRecovery_ShouldSetEnableAutoRecovery()
    {
        KafkaClientConsumerConfigurationBuilder builder = new();
        builder.DisableAutoRecovery();

        KafkaClientConsumerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnableAutoRecovery.Should().BeFalse();
    }

    [Fact]
    public void EnableAutoCommit_ShouldSetEnableAutoCommit()
    {
        KafkaClientConsumerConfigurationBuilder builder = new();
        builder.EnableAutoCommit();

        KafkaClientConsumerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnableAutoCommit.Should().BeTrue();
    }

    [Fact]
    public void EnableAutoCommit_ShouldClearCommitOffsetEach()
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
    public void DisableAutoCommit_ShouldSetEnableAutoCommit()
    {
        KafkaClientConsumerConfigurationBuilder builder = new();
        builder.DisableAutoCommit();

        KafkaClientConsumerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnableAutoCommit.Should().BeFalse();
    }

    [Fact]
    public void EnablePartitionEof_ShouldSetEnablePartitionEof()
    {
        KafkaClientConsumerConfigurationBuilder builder = new();
        builder.EnablePartitionEof();

        KafkaClientConsumerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnablePartitionEof.Should().BeTrue();
    }

    [Fact]
    public void DisablePartitionEof_ShouldSetEnablePartitionEof()
    {
        KafkaClientConsumerConfigurationBuilder builder = new();
        builder.DisablePartitionEof();

        KafkaClientConsumerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.EnablePartitionEof.Should().BeFalse();
    }

    [Fact]
    public void AllowAutoCreateTopics_ShouldSetAllowAutoCreateTopics()
    {
        KafkaClientConsumerConfigurationBuilder builder = new();
        builder.AllowAutoCreateTopics();

        KafkaClientConsumerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.AllowAutoCreateTopics.Should().BeTrue();
    }

    [Fact]
    public void DisallowAutoCreateTopics_ShouldSetAllowAutoCreateTopics()
    {
        KafkaClientConsumerConfigurationBuilder builder = new();
        builder.DisallowAutoCreateTopics();

        KafkaClientConsumerConfiguration configuration = builder.Build();

        configuration.Should().NotBeNull();
        configuration.AllowAutoCreateTopics.Should().BeFalse();
    }
}
