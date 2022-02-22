// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Wraps the <see cref="Confluent.Kafka.ProducerConfig" /> adding the Silverback specific settings.
/// </summary>
[SuppressMessage("ReSharper", "SA1623", Justification = "Comments style is in-line with Confluent.Kafka")]
public sealed partial record KafkaClientProducerConfiguration : KafkaClientConfiguration<ProducerConfig>
{
    private const bool KafkaDefaultEnableDeliveryReports = true;

    /// <summary>
    ///     Initializes a new instance of the <see cref="KafkaClientProducerConfiguration" /> class.
    /// </summary>
    /// <param name="clientConfiguration">
    ///     The <see cref="KafkaClientConfiguration" /> to be used to initialize the <see cref="KafkaClientProducerConfiguration" />.
    /// </param>
    public KafkaClientProducerConfiguration(KafkaClientConfiguration? clientConfiguration = null)
        : base(
            clientConfiguration == null
                ? new ProducerConfig()
                : new ProducerConfig(clientConfiguration.GetConfluentClientConfig().Clone()))
    {
        // Optimization: by default limit delivery report to just key and status since no other field is needed
        DeliveryReportFields = "key,status";
    }

    internal KafkaClientProducerConfiguration(ProducerConfig producerConfig)
        : base(producerConfig)
    {
    }

    /// <summary>
    ///     Gets a value indicating whether delivery reports are enabled according to the explicit configuration and Kafka defaults.
    /// </summary>
    public bool AreDeliveryReportsEnabled => EnableDeliveryReports ?? KafkaDefaultEnableDeliveryReports;

    /// <summary>
    ///     Specifies whether an exception must be thrown by the producer if the persistence is not acknowledge
    ///     by the broker. The default is <c>true</c>.
    /// </summary>
    public bool ThrowIfNotAcknowledged { get; init; } = true;

    /// <summary>
    ///     Specifies whether the producer has to be disposed and recreated if a <see cref="KafkaException" />
    ///     is thrown. The default is <c>true</c>.
    /// </summary>
    public bool DisposeOnException { get; init; } = true;

    /// <summary>
    ///     Specifies the flush operation timeout. The default is 30 seconds.
    /// </summary>
    public TimeSpan FlushTimeout { get; init; } = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     Gets a value indicating whether the persistence status will be returned as part of the
    ///     delivery reports according to the explicit configuration and Kafka defaults.
    /// </summary>
    internal bool ArePersistenceStatusReportsEnabled =>
        AreDeliveryReportsEnabled &&
        (string.IsNullOrEmpty(DeliveryReportFields) ||
         DeliveryReportFields == "all" ||
         DeliveryReportFields.Contains("status", StringComparison.Ordinal));

    /// <inheritdoc cref="IValidatableEndpointSettings.Validate" />
    public override void Validate()
    {
        if (string.IsNullOrEmpty(BootstrapServers))
            throw new EndpointConfigurationException("The bootstrap servers are required to connect with the message broker.");

        if (ThrowIfNotAcknowledged && !ArePersistenceStatusReportsEnabled)
        {
            throw new EndpointConfigurationException(
                $"{nameof(ThrowIfNotAcknowledged)} cannot be set to true if delivery reports " +
                "are not enabled and the status field isn't included. " +
                $"Set {nameof(EnableDeliveryReports)} and {nameof(DeliveryReportFields)} " +
                $"accordingly or set {nameof(ThrowIfNotAcknowledged)} to false.");
        }
    }

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(KafkaClientProducerConfiguration? other)
    {
        if (other is null)
            return false;

        if (ReferenceEquals(this, other))
            return true;

        return ThrowIfNotAcknowledged == other.ThrowIfNotAcknowledged &&
               DisposeOnException == other.DisposeOnException &&
               FlushTimeout == other.FlushTimeout &&
               ConfigurationDictionaryEqualityComparer.StringString.Equals(ClientConfig, other.ClientConfig);
    }

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() => HashCode.Combine(BootstrapServers);
}
