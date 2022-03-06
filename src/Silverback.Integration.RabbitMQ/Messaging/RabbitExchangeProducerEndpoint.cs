﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Configuration.Rabbit;

namespace Silverback.Messaging
{
    /// <summary>
    ///     The Rabbit producer configuration to produce to an exchange.
    /// </summary>
    public sealed class RabbitExchangeProducerEndpoint
        : RabbitProducerEndpoint, IEquatable<RabbitExchangeProducerEndpoint>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RabbitExchangeProducerEndpoint" /> class.
        /// </summary>
        /// <param name="name">
        ///     The name of the exchange.
        /// </param>
        public RabbitExchangeProducerEndpoint(string name)
            : base(name)
        {
        }

        /// <summary>
        ///     Gets the exchange configuration.
        /// </summary>
        public RabbitExchangeConfig Exchange { get; init; } = new();

        /// <inheritdoc cref="RabbitProducerEndpoint.Validate" />
        public override void Validate()
        {
            base.Validate();

            if (Exchange == null)
                throw new EndpointConfigurationException("Exchange cannot be null");

            Exchange.Validate();
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(RabbitExchangeProducerEndpoint? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return BaseEquals(other) && Equals(Exchange, other.Exchange);
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

            return Equals((RabbitExchangeProducerEndpoint)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        public override int GetHashCode() => HashCode.Combine(Name);
    }
}
