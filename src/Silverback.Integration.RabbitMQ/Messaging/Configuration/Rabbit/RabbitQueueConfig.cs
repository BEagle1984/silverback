﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Configuration.Rabbit
{
    /// <summary>
    ///     The RabbitMQ queue configuration.
    /// </summary>
    public sealed class RabbitQueueConfig : RabbitEndpointConfig, IEquatable<RabbitQueueConfig>
    {
        /// <summary>
        ///     Gets a value indicating whether the queue is used by only one connection and will be deleted
        ///     when that connection closes.
        /// </summary>
        public bool IsExclusive { get; init; }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(RabbitQueueConfig? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return BaseEquals(other) && IsExclusive == other.IsExclusive;
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

            return Equals((RabbitQueueConfig)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        public override int GetHashCode() => HashCode.Combine(IsDurable, IsAutoDeleteEnabled, Arguments, IsExclusive);
    }
}
