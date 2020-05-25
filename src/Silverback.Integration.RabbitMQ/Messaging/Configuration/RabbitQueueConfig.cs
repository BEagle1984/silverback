// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Configuration
{
    /// <summary> The RabbitMQ queue configuration. </summary>
    public sealed class RabbitQueueConfig : RabbitEndpointConfig, IEquatable<RabbitQueueConfig>
    {
        /// <summary>
        ///     Gets or sets a value indicating whether the queue is used by only one connection and will be deleted
        ///     when that connection closes.
        /// </summary>
        public bool IsExclusive { get; set; } = false;

        /// <inheritdoc />
        public bool Equals(RabbitQueueConfig? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return BaseEquals(other) && IsExclusive == other.IsExclusive;
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

            return Equals((RabbitQueueConfig)obj);
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode", Justification = Justifications.Settings)]
        public override int GetHashCode() => HashCode.Combine(IsDurable, IsAutoDeleteEnabled, Arguments, IsExclusive);
    }
}
