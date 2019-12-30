// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

namespace Silverback.Messaging
{
    public sealed class RabbitQueueConsumerEndpoint : RabbitConsumerEndpoint, IEquatable<RabbitQueueConsumerEndpoint>
    {
        public RabbitQueueConsumerEndpoint(string name)
            : base(name)
        {
        }
        
        public override void Validate()
        {
            base.Validate();
        }

        #region Equality

        public bool Equals(RabbitQueueConsumerEndpoint other)
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
            return Equals((RabbitQueueConsumerEndpoint) obj);
        }

        #endregion
    }
}