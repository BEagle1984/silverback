// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Configuration
{
    /// <summary> The RabbitMQ exchange configuration. </summary>
    public sealed class RabbitExchangeConfig : RabbitEndpointConfig, IEquatable<RabbitExchangeConfig>
    {
        /// <summary>
        ///     Gets or sets the exchange type. It should match with one of the constants declared in the
        ///     <see cref="RabbitMQ.Client.ExchangeType" /> static class.
        /// </summary>
        public string? ExchangeType { get; set; }

        /// <inheritdoc />
        public override void Validate()
        {
            base.Validate();

            if (string.IsNullOrEmpty(ExchangeType))
                throw new EndpointConfigurationException("ExchangeType cannot be null.");

            if (!RabbitMQ.Client.ExchangeType.All().Contains(ExchangeType))
            {
                throw new EndpointConfigurationException(
                    $"ExchangeType value is invalid. Allowed types are: ${string.Join(", ", RabbitMQ.Client.ExchangeType.All())}.");
            }
        }

        /// <inheritdoc />
        public bool Equals(RabbitExchangeConfig? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return base.Equals(other) && ExchangeType == other.ExchangeType;
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((RabbitExchangeConfig)obj);
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode", Justification = Justifications.Settings)]
        public override int GetHashCode() => HashCode.Combine(IsDurable, IsAutoDeleteEnabled, Arguments, ExchangeType);
    }
}
