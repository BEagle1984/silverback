// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Configuration
{
    public sealed class RabbitExchangeConfig : RabbitEndpointConfig, IEquatable<RabbitExchangeConfig>
    {
        /// <summary>
        ///     Gets or sets the exchange type. It should match with one of the constants declared in the
        ///     <see cref="RabbitMQ.Client.ExchangeType" /> static class.
        /// </summary>
        public string ExchangeType { get; set; }

        public override void Validate()
        {
            base.Validate();

            if (string.IsNullOrEmpty(ExchangeType))
                throw new EndpointConfigurationException("ExchangeType cannot be null.");

            if (!RabbitMQ.Client.ExchangeType.All().Contains(ExchangeType))
                throw new EndpointConfigurationException(
                    $"ExchangeType value is invalid. Allowed types are: ${string.Join(", ", RabbitMQ.Client.ExchangeType.All())}.");
        }

        #region Equality

        public bool Equals(RabbitExchangeConfig other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && ExchangeType == other.ExchangeType;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((RabbitExchangeConfig) obj);
        }

        [SuppressMessage("", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ ExchangeType.GetHashCode();
            }
        }

        #endregion
    }
}