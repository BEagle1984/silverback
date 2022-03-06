﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Extends the <see cref="Confluent.Kafka.ConsumerConfig" /> adding the Silverback specific settings.
/// </summary>
[SuppressMessage("ReSharper", "SA1623", Justification = "Comments style is in-line with Confluent.Kafka")]
public sealed class KafkaClientConsumerConfiguration : ConfluentConsumerConfigProxy, IEquatable<KafkaClientConsumerConfiguration>
{
    private const bool KafkaDefaultAutoCommitEnabled = true;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaClientConsumerConfiguration" /> class.
    /// </summary>
    /// <param name="clientConfig">
    ///     The <see cref="KafkaClientConfiguration" /> to be used to initialize the <see cref="KafkaClientConsumerConfiguration" />.
    /// </param>
    public KafkaClientConsumerConfiguration(KafkaClientConfiguration? clientConfig = null)
        : base(clientConfig?.GetConfluentConfig())
    {
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
    public int CommitOffsetEach { get; set; } = -1;

    /// <summary>
    ///     Specifies whether the consumer has to be automatically recycled when a <see cref="KafkaException" />
    ///     is thrown while polling/consuming or an issues is detected (e.g. a poll timeout is reported). The default
    ///     is <c>true</c>.
    /// </summary>
    public bool EnableAutoRecovery { get; set; } = true;

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

        if (EnableAutoOffsetStore == true)
        {
            throw new EndpointConfigurationException(
                "EnableAutoOffsetStore is not supported. " +
                "Silverback must have control over the offset storing to work properly.");
        }

        EnableAutoOffsetStore = false;
    }

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(KafkaClientConsumerConfiguration? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return CommitOffsetEach == other.CommitOffsetEach &&
               ConfluentConfigEqualityComparer.Equals(ConfluentConfig, other.ConfluentConfig);
    }

    /// <inheritdoc cref="object.Equals(object)" />
    public override bool Equals(object? obj)
    {
        if (obj is null)
            return false;

        if (ReferenceEquals(this, obj))
            return true;

        if (obj.GetType() != GetType())
            return false;

        return Equals((KafkaClientConsumerConfiguration)obj);
    }

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() => HashCode.Combine(BootstrapServers);

    /// <summary>
    ///     Returns a read-only copy of the <see cref="KafkaClientConsumerConfiguration" />.
    /// </summary>
    /// <returns>
    ///     A read-only copy of the <see cref="KafkaClientConsumerConfiguration" />.
    /// </returns>
    public KafkaClientConsumerConfiguration AsReadOnly() => this;
}
