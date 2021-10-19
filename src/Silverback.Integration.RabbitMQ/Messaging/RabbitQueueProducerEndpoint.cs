// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Configuration.Rabbit;

namespace Silverback.Messaging
{
    /// <summary>
    ///     The Rabbit producer configuration to produce to a queue.
    /// </summary>
    public sealed class RabbitQueueProducerEndpoint : RabbitProducerEndpoint, IEquatable<RabbitQueueProducerEndpoint>
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RabbitQueueProducerEndpoint" /> class.
        /// </summary>
        /// <param name="name">
        ///     The name of the queue.
        /// </param>
        public RabbitQueueProducerEndpoint(string name)
            : base(name)
        {
        }

        /// <summary>
        ///     Gets the queue configuration.
        /// </summary>
        public RabbitQueueConfig Queue { get; init; } = new();

        /// <inheritdoc cref="RabbitProducerEndpoint.Validate" />
        public override void Validate()
        {
            base.Validate();

            if (Queue == null)
                throw new EndpointConfigurationException("Queue cannot be null");

            Queue.Validate();
        }

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(RabbitQueueProducerEndpoint? other)
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

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((RabbitQueueProducerEndpoint)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        public override int GetHashCode() => HashCode.Combine(Name);
    }
}
