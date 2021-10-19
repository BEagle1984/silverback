// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging
{
    /// <summary>
    ///     The Rabbit consumer configuration to consume from a queue.
    /// </summary>
    public sealed class RabbitQueueConsumerEndpoint : RabbitConsumerEndpoint, IEquatable<RabbitQueueConsumerEndpoint>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RabbitQueueConsumerEndpoint" /> class.
        /// </summary>
        /// <param name="name">
        ///     The name of the queue.
        /// </param>
        public RabbitQueueConsumerEndpoint(string name)
            : base(name)
        {
        }

        /// <inheritdoc cref="ConsumerEndpoint.GetUniqueConsumerGroupName" />
        public override string GetUniqueConsumerGroupName() => Name;

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(RabbitQueueConsumerEndpoint? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return BaseEquals(other) && Equals(Queue, other.Queue);
        }

        /// <inheritdoc cref="object.Equals(object)" />
        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;

            if (obj.GetType() != GetType())
                return false;

            return Equals((RabbitQueueConsumerEndpoint)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        public override int GetHashCode() => HashCode.Combine(Name);
    }
}
