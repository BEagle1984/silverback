// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Configuration.Rabbit
{
    /// <summary>
    ///     The RabbitMQ exchange configuration.
    /// </summary>
    public sealed class RabbitExchangeConfig : RabbitEndpointConfig, IEquatable<RabbitExchangeConfig>
    {
        /// <summary>
        ///     Gets the exchange type. It should match with one of the constants declared in the
        ///     <see cref="RabbitMQ.Client.ExchangeType" /> static class.
        /// </summary>
        public string? ExchangeType { get; init; }

        /// <inheritdoc cref="RabbitEndpointConfig.Validate" />
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

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(RabbitExchangeConfig? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return BaseEquals(other) && ExchangeType == other.ExchangeType;
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

            return Equals((RabbitExchangeConfig)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        public override int GetHashCode() => HashCode.Combine(IsDurable, IsAutoDeleteEnabled, Arguments, ExchangeType);
    }
}
