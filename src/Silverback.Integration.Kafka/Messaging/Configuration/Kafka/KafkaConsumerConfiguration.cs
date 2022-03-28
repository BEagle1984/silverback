// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Confluent.Kafka;
using Silverback.Collections;
using Silverback.Configuration;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Wraps the <see cref="Confluent.Kafka.ConsumerConfig" /> adding the Silverback specific settings.
/// </summary>
[SuppressMessage("ReSharper", "SA1623", Justification = "Comments style is in-line with Confluent.Kafka")]
public sealed partial record KafkaConsumerConfiguration : KafkaClientConfiguration<ConsumerConfig>
{
    private const bool KafkaDefaultAutoCommitEnabled = true;

    private readonly bool _processPartitionsIndependently = true;

    private readonly IValueReadOnlyCollection<KafkaConsumerEndpointConfiguration> _endpoints =
        ValueReadOnlyCollection.Empty<KafkaConsumerEndpointConfiguration>();

    internal KafkaConsumerConfiguration(ConsumerConfig? consumerConfig = null)
        : base(consumerConfig)
    {
        // This property is not exposed and it's hardcoded to false
        ClientConfig.EnableAutoOffsetStore = false;
    }

    /// <summary>
    ///     Gets a value indicating whether autocommit is enabled according to the explicit
    ///     configuration and Kafka defaults.
    /// </summary>
    public bool IsAutoCommitEnabled => EnableAutoCommit ?? KafkaDefaultAutoCommitEnabled;

    /// <summary>
    ///     Defines the number of message to be processed before committing the offset to the server. The most
    ///     reliable level is 1 but it reduces throughput.
    /// </summary>
    public int? CommitOffsetEach { get; init; }

    /// <summary>
    ///     Specifies whether the consumer has to be automatically recycled when a <see cref="KafkaException" />
    ///     is thrown while polling/consuming or an issues is detected (e.g. a poll timeout is reported). The default
    ///     is <c>true</c>.
    /// </summary>
    public bool EnableAutoRecovery { get; init; } = true;

    /// <summary>
    ///     Gets a value indicating whether the partitions must be processed independently.
    ///     When <c>true</c> a stream will published per each partition and the sequences (<see cref="ChunkSequence" />,
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
    ///     The default is 2.
    /// </summary>
    public int BackpressureLimit { get; init; } = 2;

    // /// <summary>
    // ///    Gets a value indicating whether the consumed offsets must be committed to the broker. The default is <c>true</c>.
    // /// </summary>
    // public bool CommitOffsets { get; init; } = true;

    // /// <summary>
    // ///     Gets the <see cref="IOffsetStore" /> to be used to store the offsets. The stored offsets will be used during the partitions assignment to determine the starting offset and ensure
    // /// </summary>
    // public IOffsetStore? ClientSideOffsetStore { get;init; }

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
        ValidateEndpoints();

        CheckDuplicateTopics();

        // TODO: TO BE REFINED to include other cases where the GroupId is needed
        if (!IsStaticAssignment && string.IsNullOrEmpty(GroupId))
            throw new BrokerConfigurationException("A group id must be specified when the partitions are assigned dynamically.");

        if (string.IsNullOrEmpty(BootstrapServers))
            throw new BrokerConfigurationException("The bootstrap servers are required to connect with the message broker.");

        if (IsAutoCommitEnabled && CommitOffsetEach != null)
            throw new BrokerConfigurationException($"{nameof(CommitOffsetEach)} cannot be used when auto-commit is enabled. Explicitly disable it setting {nameof(EnableAutoCommit)} to false.");

        if (!IsAutoCommitEnabled && CommitOffsetEach is null or < 1)
            throw new BrokerConfigurationException($"{nameof(CommitOffsetEach)} must be greater or equal to 1 when auto-commit is disabled.");

        if (MaxDegreeOfParallelism < 1)
            throw new BrokerConfigurationException("The specified degree of parallelism must be greater or equal to 1.");

        if (MaxDegreeOfParallelism > 1 && !_processPartitionsIndependently)
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

        List<string> topics = Endpoints.SelectMany(GetDistinctTopicNames).ToList();

        if (topics.Count != topics.Distinct().Count())
            throw new BrokerConfigurationException("Cannot connect to the same topic in different endpoints in the same consumer.");
    }
}
