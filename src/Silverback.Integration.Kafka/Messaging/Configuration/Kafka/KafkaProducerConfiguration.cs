// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using Silverback.Collections;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     The <see cref="KafkaProducer" /> configuration.
/// </summary>
public sealed partial record KafkaProducerConfiguration : KafkaClientConfiguration<ProducerConfig>
{
    private const bool KafkaDefaultEnableDeliveryReports = true;

    internal KafkaProducerConfiguration(ProducerConfig? producerConfig = null)
        : base(producerConfig)
    {
        // Optimization: by default limit delivery report to just key and status since no other field is needed
        DeliveryReportFields = "key,status";
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
    ///     Gets the configured endpoints.
    /// </summary>
    public IValueReadOnlyCollection<KafkaProducerEndpointConfiguration> Endpoints { get; init; } = ValueReadOnlyCollection.Empty<KafkaProducerEndpointConfiguration>();

    /// <summary>
    ///     Gets a value indicating whether the persistence status will be returned as part of the
    ///     delivery reports according to the explicit configuration and Kafka defaults.
    /// </summary>
    internal bool ArePersistenceStatusReportsEnabled =>
        AreDeliveryReportsEnabled &&
        (string.IsNullOrEmpty(DeliveryReportFields) ||
         DeliveryReportFields == "all" ||
         DeliveryReportFields.Contains("status", StringComparison.Ordinal));

    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public override void Validate()
    {
        if (Endpoints == null || Endpoints.Count == 0)
            throw new BrokerConfigurationException("At least one endpoint must be configured.");

        Endpoints.ForEach(endpoint => endpoint.Validate());

        if (string.IsNullOrEmpty(BootstrapServers))
            throw new BrokerConfigurationException("The bootstrap servers are required to connect with the message broker.");

        if (ThrowIfNotAcknowledged && !ArePersistenceStatusReportsEnabled)
        {
            throw new BrokerConfigurationException(
                $"{nameof(ThrowIfNotAcknowledged)} cannot be set to true if delivery reports " +
                "are not enabled and the status field isn't included. " +
                $"Set {nameof(EnableDeliveryReports)} and {nameof(DeliveryReportFields)} " +
                $"accordingly or set {nameof(ThrowIfNotAcknowledged)} to false.");
        }
    }
}
