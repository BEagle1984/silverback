// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

namespace Silverback.Messaging
{
    public abstract class RabbitConsumerEndpoint : ConsumerEndpoint
    {
        protected RabbitConsumerEndpoint(string name) : base(name)
        {
        }

        /// <summary>
        /// Gets or sets the RabbitMQ connection settings.
        /// </summary>
        public RabbitConnectionConfig Connection { get; set; } = new RabbitConnectionConfig();

        public override void Validate()
        {
            base.Validate();

            if (Connection == null)
                throw new EndpointConfigurationException("Connection cannot be null");

            Connection.Validate();
        }

        #region Equality

        protected bool Equals(RabbitConsumerEndpoint other) => 
            base.Equals(other) && Equals(Connection, other.Connection);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((RabbitConsumerEndpoint) obj);
        }

        #endregion
    }
}