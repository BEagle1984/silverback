// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;

namespace Silverback.Messaging.Configuration.Kafka
{
    /// <summary>
    ///     Extends the <see cref="Confluent.Kafka.ProducerConfig" /> adding the Silverback specific settings.
    /// </summary>
    [SuppressMessage("ReSharper", "SA1623", Justification = "Comments style is in-line with Confluent.Kafka")]
    public sealed class KafkaProducerConfig : ConfluentProducerConfigProxy, IEquatable<KafkaProducerConfig>
    {
        private const bool KafkaDefaultEnableDeliveryReports = true;

        /// <summary>
        ///     Initializes a new instance of the <see cref="KafkaProducerConfig" /> class.
        /// </summary>
        /// <param name="clientConfig">
        ///     The <see cref="KafkaClientConfig" /> to be used to initialize the
        ///     <see cref="KafkaProducerConfig" />.
        /// </param>
        public KafkaProducerConfig(KafkaClientConfig? clientConfig = null)
            : base(clientConfig?.GetConfluentConfig())
        {
        }

        /// <summary>
        ///     Gets a value indicating whether delivery reports are enabled according to the explicit
        ///     configuration and Kafka defaults.
        /// </summary>
        public bool AreDeliveryReportsEnabled => EnableDeliveryReports ?? KafkaDefaultEnableDeliveryReports;

        /// <summary>
        ///     Specifies whether an exception must be thrown by the producer if the persistence is not acknowledge
        ///     by the broker. The default is <c>true</c>.
        /// </summary>
        public bool ThrowIfNotAcknowledged { get; set; } = true;

        /// <summary>
        ///     Specifies whether the producer has to be disposed and recreated if a <see cref="KafkaException" />
        ///     is thrown. The default is <c>false</c>.
        /// </summary>
        public bool DisposeOnException { get; set; } = true;

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
        // TODO: Test validation
        public override void Validate()
        {
            if (string.IsNullOrEmpty(BootstrapServers))
            {
                throw new EndpointConfigurationException(
                    "BootstrapServers is required to connect with the message broker.");
            }

            if (ThrowIfNotAcknowledged && !ArePersistenceStatusReportsEnabled)
            {
                throw new EndpointConfigurationException(
                    "Configuration.ThrowIfNotAcknowledged cannot be set to true delivery reports are not " +
                    "enabled and the status field isn't included. " +
                    "Set Configuration.EnableDeliveryReports and Configuration.DeliveryReportFields" +
                    "accordingly or set Configuration.ThrowIfNotAcknowledged to false.");
            }
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(KafkaProducerConfig? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return ConfluentConfigEqualityComparer.Equals(ConfluentConfig, other.ConfluentConfig);
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

            return Equals((KafkaProducerConfig)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        public override int GetHashCode() => 0;
    }
}
