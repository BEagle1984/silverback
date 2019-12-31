// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

namespace Silverback.Messaging
{
    public sealed class RabbitQueueProducerEndpoint : RabbitProducerEndpoint, IEquatable<RabbitQueueProducerEndpoint>
    {
        public RabbitQueueProducerEndpoint(string name)
            : base(name)
        {
        }

        /// <summary>
        ///     Gets or sets the queue configuration.
        /// </summary>
        public RabbitQueueConfig Queue { get; set; } = new RabbitQueueConfig();

        public override void Validate()
        {
            base.Validate();

            if (Queue == null)
                throw new EndpointConfigurationException("Queue cannot be null");

            Queue.Validate();
        }

        #region Equality

        public bool Equals(RabbitQueueProducerEndpoint other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && Equals(Queue, other.Queue);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((RabbitQueueProducerEndpoint) obj);
        }

        #endregion
    }
}