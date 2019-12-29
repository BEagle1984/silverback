// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Configuration
{
    public sealed class RabbitQueueConfig : RabbitEndpointConfig, IEquatable<RabbitQueueConfig>
    {
        /// <summary>
        /// Gets or sets a boolean value indicating whether the queue is used by only one connection and
        /// will be deleted when that connection closes.
        /// </summary>
        public bool IsExclusive { get; set; }

        #region Equality

        public bool Equals(RabbitQueueConfig other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return base.Equals(other) && IsExclusive == other.IsExclusive;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((RabbitQueueConfig) obj);
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                return (base.GetHashCode() * 397) ^ IsExclusive.GetHashCode();
            }
        }

        #endregion
    }
}