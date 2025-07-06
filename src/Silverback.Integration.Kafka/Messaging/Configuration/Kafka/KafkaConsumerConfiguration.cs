// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Confluent.Kafka;
using Silverback.Collections;
using Silverback.Configuration;
using Silverback.Messaging.Consuming.KafkaOffsetStore;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Wraps the <see cref="Confluent.Kafka.ConsumerConfig" /> adding the Silverback specific settings.
/// </summary>
public sealed partial record KafkaConsumerConfiguration : KafkaClientConfiguration<ConsumerConfig>
{
    internal const string UnsetGroupId = "not-set";

    private readonly bool _commitOffsets = true;

    private readonly bool _sendOffsetsToTransaction;

    private readonly bool _processPartitionsIndependently = true;

    private readonly IValueReadOnlyCollection<KafkaConsumerEndpointConfiguration> _endpoints = ValueReadOnlyCollection.Empty<KafkaConsumerEndpointConfiguration>();

    /// <summary>
    ///     Gets the client group id. All clients sharing the same group id belong to the same group. The default is <c>null</c>
    ///     (which will internally be replaced with <c>"not-set"</c> since the underlying library requires a value).
    /// </summary>
    public string? GroupId { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the offsets must be committed. The default is <c>true</c>.
    /// </summary>
    public bool CommitOffsets
    {
        get => _commitOffsets;
        init
        {
            _commitOffsets = value;

            if (!value)
            {
                EnableAutoCommit = false;
                CommitOffsetEach = null;
            }
        }
    }

    /// <summary>
    ///     Gets the settings for the <see cref="IKafkaOffsetStore" /> to be used to store the offsets. The stored offsets will be used during
    ///     the partitions assignment to determine the starting offset and ensure that each message is consumed only once.
    /// </summary>
    public KafkaOffsetStoreSettings? ClientSideOffsetStore { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the offsets must be automatically and periodically committed in the background.<br />
    ///     Note: setting this to false does not prevent the consumer from fetching previously committed start offsets. To circumvent this
    ///     behaviour set specific start offsets per partition in the call to assign(). The default is <c>true</c>.
    /// </summary>
    public bool EnableAutoCommit { get; init; } = true;

    /// <summary>
    ///     Gets the number of messages to be processed before committing the offset to the server. The most
    ///     reliable level is 1, but it reduces throughput.
    /// </summary>
    public int? CommitOffsetEach { get; init; }

    /// <summary>
    ///     Gets a value indicating whether the consumer should commit the consumed offsets in the same transaction of the produced
    ///     messages. The default is <c>false</c>.
    /// </summary>
    public bool SendOffsetsToTransaction
    {
        get => _sendOffsetsToTransaction;
        init
        {
            _sendOffsetsToTransaction = value;

            if (value)
                CommitOffsets = false;
        }
    }

    /// <summary>
    ///     Gets a value indicating whether the consumer has to be automatically recycled when a <see cref="KafkaException" />
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
        get => _processPartitionsIndependently;
        init
        {
            _processPartitionsIndependently = value;

            if (!value)
                MaxDegreeOfParallelism = 1;
        }
    }

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
    ///     Gets the timeout to wait for the metadata to be retrieved from the broker. The default is 30 seconds.
    /// </summary>
    public TimeSpan GetMetadataTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Gets the timeout to wait for the consumer to poll for new messages before initiating a new poll. The default is 500 milliseconds.
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
        get => _endpoints;
        init
        {
            _endpoints = value;
            IsStaticAssignment = _endpoints is { Count: >= 1 } && _endpoints.First().IsStaticAssignment;
        }
    }

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

        if (MaxDegreeOfParallelism > 1 && !_processPartitionsIndependently)
            throw new BrokerConfigurationException($"{nameof(MaxDegreeOfParallelism)} cannot be greater than 1 when the partitions aren't processed independently.");

        if (BackpressureLimit < 1)
            throw new BrokerConfigurationException("The backpressure limit must be greater or equal to 1.");

        if (GetMetadataTimeout <= TimeSpan.Zero)
            throw new BrokerConfigurationException("The get metadata timeout must be greater than 0.");

        if (!ProcessPartitionsIndependently && Endpoints.Skip(0).Any(endpoint => endpoint != Endpoints.First()))
            throw new BrokerConfigurationException("All endpoints must use the same Batch settings if the partitions are consumed independently.");
    }

    internal override ConsumerConfig ToConfluentConfig()
    {
        ConsumerConfig confluentConfig = MapCore();

        confluentConfig.GroupId = string.IsNullOrEmpty(GroupId) ? UnsetGroupId : GroupId;
        confluentConfig.EnableAutoCommit = EnableAutoCommit;

        // This property is not exposed and hardcoded: the offsets will be explicitly stored only after successful processing
        confluentConfig.EnableAutoOffsetStore = false;

        return confluentConfig;
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

        List<string> topics = Endpoints.SelectMany(GetDistinctTopicNames).ToList();

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
