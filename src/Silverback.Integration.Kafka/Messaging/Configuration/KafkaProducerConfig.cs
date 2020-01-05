// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using Silverback.Util;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

namespace Silverback.Messaging.Configuration
{
    public sealed class KafkaProducerConfig : ConfluentProducerConfigProxy, IEquatable<KafkaProducerConfig>
    {
        private const bool KafkaDefaultEnableDeliveryReports = true;

        /// <summary>
        ///     Gets a boolean value indicating whether delivery reports are enabled according
        ///     to the explicit configuration and Kafka defaults.
        /// </summary>
        public bool AreDeliveryReportsEnabled => EnableDeliveryReports ?? KafkaDefaultEnableDeliveryReports;

        /// <summary>
        ///     Gets a boolean value indicating whether the persistence status will be returned
        ///     as part of the delivery reports according to the explicit configuration
        ///     and Kafka defaults.
        /// </summary>
        internal bool ArePersistenceStatusReportsEnabled =>
            AreDeliveryReportsEnabled &&
            (string.IsNullOrEmpty(DeliveryReportFields) ||
             DeliveryReportFields == "all" ||
             DeliveryReportFields.Contains("status"));

        /// <summary>
        ///     Gets or sets a boolean value indicating whether an exception must be thrown
        ///     by the producer if the persistence is not acknowledge by the broker.
        /// </summary>
        public bool ThrowIfNotAcknowledged { get; set; } = true;

        /// <summary>
        ///     Specifies whether the producer has to be disposed and recreated if a
        ///     <see cref="KafkaException" /> is thrown (default is <c>false</c>).<br />
        /// </summary>
        public bool DisposeOnException { get; set; } = true;

        public override void Validate()
        {
            if (ThrowIfNotAcknowledged && !ArePersistenceStatusReportsEnabled)
            {
                throw new EndpointConfigurationException(
                    "Configuration.ThrowIfNotAcknowledged cannot be set to true delivery reports are not " +
                    "enabled and the status field isn't included. " +
                    "Set Configuration.EnableDeliveryReports and Configuration.DeliveryReportFields" +
                    "accordingly or set Configuration.ThrowIfNotAcknowledged to false.");
            }
        }

        #region Equality

        public bool Equals(KafkaProducerConfig other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return ConfluentConfigComparer.Equals(ConfluentConfig, other.ConfluentConfig);
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((KafkaProducerConfig) obj);
        }

        #endregion
    }
}