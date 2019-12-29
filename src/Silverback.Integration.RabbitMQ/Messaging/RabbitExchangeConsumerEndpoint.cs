// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

namespace Silverback.Messaging
{
    public sealed class RabbitExchangeConsumerEndpoint : RabbitConsumerEndpoint, IEquatable<RabbitExchangeConsumerEndpoint>
    {
        public RabbitExchangeConsumerEndpoint(string name)
            : base(name)
        {
        }

        /// <summary>
        /// Gets or sets the exchange configuration.
        /// </summary>
        public RabbitExchangeConfig Exchange { get; set; } = new RabbitExchangeConfig();

        public override void Validate()
        {
            base.Validate();
            
            if (Exchange == null)
                throw new EndpointConfigurationException("Exchange cannot be null");

            Exchange.Validate();
        }

        #region Equality

        public bool Equals(RabbitExchangeConsumerEndpoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && Equals(Exchange, other.Exchange);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((RabbitExchangeConsumerEndpoint) obj);
        }

        #endregion
    }
}