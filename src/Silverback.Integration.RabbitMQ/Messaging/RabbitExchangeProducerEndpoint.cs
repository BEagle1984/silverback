// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

namespace Silverback.Messaging
{
    /// <summary> Represents an exchange to produce to. </summary>
    public sealed class RabbitExchangeProducerEndpoint
        : RabbitProducerEndpoint, IEquatable<RabbitExchangeProducerEndpoint>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RabbitExchangeProducerEndpoint" /> class.
        /// </summary>
        /// <param name="name"> The name of the exchange. </param>
        public RabbitExchangeProducerEndpoint(string name)
            : base(name)
        {
        }

        /// <summary> Gets or sets the exchange configuration. </summary>
        public RabbitExchangeConfig Exchange { get; set; } = new RabbitExchangeConfig();

        /// <inheritdoc />
        public override void Validate()
        {
            base.Validate();

            if (Exchange == null)
                throw new EndpointConfigurationException("Exchange cannot be null");

            Exchange.Validate();
        }

        /// <inheritdoc />
        public bool Equals(RabbitExchangeProducerEndpoint other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return base.Equals(other) && Equals(Exchange, other.Exchange);
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

            return Equals((RabbitExchangeProducerEndpoint)obj);
        }
    }
}
