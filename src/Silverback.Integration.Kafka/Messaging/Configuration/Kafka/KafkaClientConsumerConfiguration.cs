// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Wraps the <see cref="Confluent.Kafka.ConsumerConfig" /> adding the Silverback specific settings.
/// </summary>
[SuppressMessage("ReSharper", "SA1623", Justification = "Comments style is in-line with Confluent.Kafka")]
public sealed partial record KafkaClientConsumerConfiguration : KafkaClientConfiguration<ConsumerConfig>
{
    private const bool KafkaDefaultAutoCommitEnabled = true;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaClientConsumerConfiguration" /> class.
    /// </summary>
    /// <param name="clientConfiguration">
    ///     The <see cref="KafkaClientConfiguration" /> to be used to initialize the <see cref="KafkaClientConsumerConfiguration" />.
    /// </param>
    public KafkaClientConsumerConfiguration(KafkaClientConfiguration? clientConfiguration = null)
        : base(
            clientConfiguration == null
                ? new ConsumerConfig()
                : new ConsumerConfig(clientConfiguration.GetConfluentClientConfig().Clone()))
    {
        // This property is not exposed and it's hardcoded to false
        ClientConfig.EnableAutoOffsetStore = false;
    }

    internal KafkaClientConsumerConfiguration(ConsumerConfig consumerConfig)
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
    ///     Throws an <see cref="EndpointConfigurationException" /> if the current configuration is not valid.
    /// </summary>
    internal void Validate(bool isStaticAssignment)
    {
        // TODO: TO BE REFINED to include other cases where the GroupId is needed
        if (!isStaticAssignment && string.IsNullOrEmpty(GroupId))
        {
            throw new EndpointConfigurationException(
                "A group id must be specified when the partitions are assigned dynamically.",
                GroupId,
                nameof(GroupId));
        }

        Validate();
    }

    /// <inheritdoc cref="IValidatableEndpointSettings.Validate" />
    public override void Validate()
    {
        if (string.IsNullOrEmpty(BootstrapServers))
        {
            throw new EndpointConfigurationException(
                "The bootstrap servers are required to connect with the message broker.",
                BootstrapServers,
                nameof(BootstrapServers));
        }

        if (IsAutoCommitEnabled && CommitOffsetEach >= 0)
        {
            throw new EndpointConfigurationException(
                $"{nameof(CommitOffsetEach)} cannot be used when auto-commit is enabled. " +
                $"Explicitly disable it setting {nameof(EnableAutoCommit)} to false.",
                CommitOffsetEach,
                nameof(CommitOffsetEach));
        }

        if (!IsAutoCommitEnabled && CommitOffsetEach <= 0)
        {
            throw new EndpointConfigurationException(
                $"{nameof(CommitOffsetEach)} must be greater or equal to 1 when auto-commit is disabled.",
                CommitOffsetEach,
                nameof(CommitOffsetEach));
        }

        if (ClientConfig == null)
        {
            throw new EndpointConfigurationException(
                "The client configuration is required to connect with the message broker.",
                ClientConfig,
                nameof(ClientConfig));
        }
    }

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(KafkaClientConsumerConfiguration? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return CommitOffsetEach == other.CommitOffsetEach &&
               EnableAutoRecovery == other.EnableAutoRecovery &&
               ConfigurationDictionaryEqualityComparer.StringString.Equals(ClientConfig, other.ClientConfig);
    }

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() => HashCode.Combine(BootstrapServers);
}
