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
public sealed partial record KafkaClientConsumerConfiguration
{
    private readonly ConsumerConfig _clientConfig;

    private const bool KafkaDefaultAutoCommitEnabled = true;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaClientConsumerConfiguration" /> class.
    /// </summary>
    /// <param name="clientConfig">
    ///     The <see cref="KafkaClientConfiguration" /> to be used to initialize the <see cref="KafkaClientConsumerConfiguration" />.
    /// </param>
    public KafkaClientConsumerConfiguration(KafkaClientConfiguration? clientConfig = null)
        : this(clientConfig?.GetConfluentClientConfig() ?? new ClientConfig())
    {
    }

    internal KafkaClientConsumerConfiguration(ClientConfig clientConfig)
        : this(new ConsumerConfig(clientConfig.Clone()))
    {
    }

    private KafkaClientConsumerConfiguration(ConsumerConfig clientConfig)
        : base(clientConfig)
    {
        _clientConfig = clientConfig;
        _clientConfig.EnableAutoOffsetStore = false;
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

    /// <inheritdoc cref="IValidatableEndpointSettings.Validate" />
    public override void Validate()
    {
        if (string.IsNullOrEmpty(BootstrapServers))
        {
            throw new EndpointConfigurationException("BootstrapServers is required to connect with the message broker.");
        }

        if (IsAutoCommitEnabled && CommitOffsetEach >= 0)
        {
            throw new EndpointConfigurationException(
                "CommitOffsetEach cannot be used when auto-commit is enabled. " +
                "Explicitly disable it setting Configuration.EnableAutoCommit = false.");
        }

        if (!IsAutoCommitEnabled && CommitOffsetEach <= 0)
        {
            throw new EndpointConfigurationException("CommitOffSetEach must be greater or equal to 1 when auto-commit is disabled.");
        }

        if (_clientConfig.EnableAutoOffsetStore == true)
        {
            throw new EndpointConfigurationException(
                "EnableAutoOffsetStore is not supported. " +
                "Silverback must have control over the offset storing to work properly.");
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
               ConfigurationDictionaryEqualityComparer.StringString.Equals(_clientConfig, other._clientConfig);
    }

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() => HashCode.Combine(BootstrapServers);

    internal new ConsumerConfig GetConfluentClientConfig() => _clientConfig;
}
