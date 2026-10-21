// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.Collections;
using Silverback.Configuration;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Wraps the consumer configuration adding the Silverback specific settings.
/// </summary>
/// <remarks>
///     When adding a client setting:
///     1. Add the property here
///     2. Add the field, the method and the <c>Build</c> mapping in <see cref="KafkaConsumerConfigurationBuilder" />
///     3. Map it in <c>DekafClientsFactory</c>
///     4. Map it in <c>InMemoryDekafClientsFactory</c> (Silverback.Integration.Dekaf.Testing), if supported
///     5. List it in the Dekaf documentation page (docs/guides/broker/dekaf.md).
///     The settings keep the names of the Confluent.Kafka based integration (e.g. <see cref="AutoCommitIntervalMs" />),
///     so that the configuration code can be shared.
/// </remarks>
public sealed record KafkaConsumerConfiguration : KafkaClientConfiguration
{
    /// <summary>
    ///     Gets the client group id. All clients sharing the same group id belong to the same group. The default is <c>null</c>,
    ///     which is only allowed when the offsets are not committed.
    /// </summary>
    public string? GroupId { get; init; }

    /// <summary>
    ///     Gets the static group instance id, used to enable static group membership.
    /// </summary>
    public string? GroupInstanceId { get; init; }

    /// <summary>
    ///     Gets the action to take when there is no initial offset in the offset store or the desired offset is out of
    ///     range. The default is <see cref="Kafka.AutoOffsetReset.Latest" />.
    /// </summary>
    public AutoOffsetReset? AutoOffsetReset { get; init; }

    /// <summary>
    ///     Gets the frequency in milliseconds at which the stored offsets are committed when auto-commit is enabled.
    /// </summary>
    public int? AutoCommitIntervalMs { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the partition EOF event must be emitted whenever the consumer reaches the end
    ///     of a partition.
    /// </summary>
    public bool? EnablePartitionEof { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the offsets must be committed. The default is <c>true</c>.
    /// </summary>
    public bool CommitOffsets
    {
        get;
        init
        {
            field = value;

            if (!value)
            {
                EnableAutoCommit = false;
                CommitOffsetEach = null;
            }
        }
    } = true;

    /// <summary>
    ///     Gets the settings for the <see cref="IKafkaOffsetStore" /> to be used to store the offsets. The stored offsets will be used during
    ///     the partitions assignment to determine the starting offset and ensure that each message is consumed only once.
    /// </summary>
    public KafkaOffsetStoreSettings? ClientSideOffsetStore { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the offsets must be automatically and periodically committed in the background.<br />
    ///     Note: setting this to false does not prevent the consumer from fetching previously committed start offsets. To circumvent this
    ///     behaviour use a static partition assignment with explicit start offsets. The default is <c>true</c>.
    /// </summary>
    public bool EnableAutoCommit { get; init; } = true;

    /// <summary>
    ///     Gets the number of messages to be processed before committing the offset to the server. The most
    ///     reliable level is 1, but it reduces throughput.
    /// </summary>
    /// <remarks>
    ///     As with automatic commits, Kafka commit failures are logged without interrupting consumption or invoking message error policies.
    ///     Uncommitted messages may be processed again after a restart or rebalance.
    /// </remarks>
    public int? CommitOffsetEach { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the consumer should commit the consumed offsets in the same transaction of the produced
    ///     messages. The default is <c>false</c>.
    /// </summary>
    public bool SendOffsetsToTransaction
    {
        get;
        init
        {
            field = value;

            if (value)
                CommitOffsets = false;
        }
    }

    /// <summary>
    ///     Gets a value indicating whether the consumer has to be automatically recycled when a <c>Dekaf.Errors.KafkaException</c>
    ///     is thrown while polling/consuming or an issues is detected (e.g. a poll timeout is reported). The default is <c>true</c>.
    /// </summary>
    public bool EnableAutoRecovery { get; init; } = true;

    /// <summary>
    ///     Gets a value indicating whether the partitions must be processed independently.
    ///     When <c>true</c> a stream will be published per each partition and the sequences (<see cref="ChunkSequence" />,
    ///     <see cref="BatchSequence" />, ...) cannot span across the partitions.
    ///     The default is <c>true</c>.
    /// </summary>
    /// <remarks>
    ///     Settings this value to <c>false</c> implicitly sets the <see cref="MaxDegreeOfParallelism" /> to 1.
    /// </remarks>
    public bool ProcessPartitionsIndependently
    {
        get;
        init
        {
            field = value;

            if (!value)
                MaxDegreeOfParallelism = 1;
        }
    } = true;

    /// <summary>
    ///     Gets the maximum number of incoming message that can be processed concurrently. Up to a
    ///     message per each subscribed partition can be processed in parallel when processing them independently.
    ///     The default is 100.
    /// </summary>
    public int MaxDegreeOfParallelism { get; init; } = 100;

    /// <summary>
    ///     Gets the maximum number of messages to be consumed and enqueued waiting to be processed.
    ///     When <see cref="ProcessPartitionsIndependently" /> is set to <c>true</c> (default) the limit will be applied per partition.
    ///     The default is 50.
    /// </summary>
    public int BackpressureLimit { get; init; } = 50;

    /// <summary>
    ///     Gets the timeout to wait for the consumer to poll for new messages before initiating a new poll. The default is 100 milliseconds.
    /// </summary>
    public TimeSpan PollingTimeout { get; init; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    ///     Gets the maximum time to wait for a message to be consumed before the consumer is considered stale. A stale consumer is
    ///     automatically restarted.
    ///     The default is <c>null</c>, which means that the consumer is never considered stale and is never restarted.
    /// </summary>
    public TimeSpan? StallDetectionThreshold { get; init; }

    /// <summary>
    ///     Gets the configured endpoints.
    /// </summary>
    public IValueReadOnlyCollection<KafkaConsumerEndpointConfiguration> Endpoints
    {
        get;
        init
        {
            field = value;
            IsStaticAssignment = field is { Count: >= 1 } && field.First().IsStaticAssignment;
        }
    } = ValueReadOnlyCollection.Empty<KafkaConsumerEndpointConfiguration>();

    /// <summary>
    ///     Gets a value indicating whether the consumer is configured with a static partition assignment.
    /// </summary>
    internal bool IsStaticAssignment { get; private set; }

    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public override void Validate()
    {
        base.Validate();

        ValidateEndpoints();

        CheckDuplicateTopics();

        if (string.IsNullOrEmpty(GroupId))
            CheckGroupIdRequirements();

        ValidateCommitStrategy();

        if (MaxDegreeOfParallelism < 1)
            throw new BrokerConfigurationException($"{nameof(MaxDegreeOfParallelism)} must be greater or equal to 1.");

        if (MaxDegreeOfParallelism > 1 && !ProcessPartitionsIndependently)
            throw new BrokerConfigurationException($"{nameof(MaxDegreeOfParallelism)} cannot be greater than 1 when the partitions aren't processed independently.");

        if (BackpressureLimit < 1)
            throw new BrokerConfigurationException("The backpressure limit must be greater or equal to 1.");

        if (!ProcessPartitionsIndependently && Endpoints.Skip(0).Any(endpoint => endpoint != Endpoints.First()))
            throw new BrokerConfigurationException("All endpoints must use the same Batch settings if the partitions are consumed independently.");
    }

    private void ValidateEndpoints()
    {
        if (Endpoints == null || Endpoints.Count == 0)
            throw new BrokerConfigurationException("At least one endpoint must be configured.");

        Endpoints.ForEach(endpoint => endpoint.Validate());

        if (Endpoints.Any(endpoint => endpoint.IsStaticAssignment) && Endpoints.Any(endpoint => !endpoint.IsStaticAssignment))
            throw new BrokerConfigurationException("Cannot mix static partition assignments and subscriptions in the same consumer.");
    }

    private void CheckDuplicateTopics()
    {
        static IEnumerable<string> GetDistinctTopicNames(KafkaConsumerEndpointConfiguration endpoint) =>
            endpoint.TopicPartitions.Select(topicPartition => topicPartition.Topic).Distinct();

        List<string> topics = [.. Endpoints.SelectMany(GetDistinctTopicNames)];

        if (topics.Count != topics.Distinct().Count())
            throw new BrokerConfigurationException("Cannot connect to the same topic in different endpoints in the same consumer.");
    }

    private void CheckGroupIdRequirements()
    {
        if (!IsStaticAssignment)
        {
            throw new BrokerConfigurationException(
                $"The {nameof(GroupId)} must be specified when the partitions are assigned dynamically. " +
                $"Explicitly specify the partitions to be consumed or set the {nameof(GroupId)}.");
        }

        if (CommitOffsets)
        {
            throw new BrokerConfigurationException(
                $"The {nameof(GroupId)} should be specified when committing the offsets to the broker. " +
                $"Set {nameof(CommitOffsets)} to false or set the {nameof(GroupId)}.");
        }

        if (ClientSideOffsetStore != null)
            throw new BrokerConfigurationException($"The {nameof(GroupId)} should be specified when using a client side offset store.");
    }

    private void ValidateCommitStrategy()
    {
        if (CommitOffsets)
        {
            if (EnableAutoCommit && CommitOffsetEach != null)
                throw new BrokerConfigurationException($"{nameof(CommitOffsetEach)} cannot be used when auto-commit is enabled. Explicitly disable it setting {nameof(EnableAutoCommit)} to false.");

            if (!EnableAutoCommit && CommitOffsetEach is null or < 1)
                throw new BrokerConfigurationException($"{nameof(CommitOffsetEach)} must be greater or equal to 1 when auto-commit is disabled.");

            if (SendOffsetsToTransaction)
                throw new BrokerConfigurationException($"{nameof(SendOffsetsToTransaction)} and {nameof(CommitOffsets)} cannot be enabled at the same time.");
        }
        else
        {
            if (EnableAutoCommit || CommitOffsetEach != null)
                throw new BrokerConfigurationException($"Auto commit and {nameof(CommitOffsetEach)} shouldn't be enabled when not committing. Set {nameof(EnableAutoCommit)} to false and {nameof(CommitOffsets)} to null, or {nameof(CommitOffsets)} to true.");
        }
    }
}
