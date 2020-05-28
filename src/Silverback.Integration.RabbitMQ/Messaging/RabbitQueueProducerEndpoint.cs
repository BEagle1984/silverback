// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Configuration;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

namespace Silverback.Messaging
{
    /// <summary>
    ///     Represents a queue to produce to.
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
        ///     Gets or sets the queue configuration.
        /// </summary>
        public RabbitQueueConfig Queue { get; set; } = new RabbitQueueConfig();

        /// <inheritdoc />
        public override void Validate()
        {
            base.Validate();

            if (Queue == null)
                throw new EndpointConfigurationException("Queue cannot be null");

            Queue.Validate();
        }

        /// <inheritdoc />
        public bool Equals(RabbitQueueProducerEndpoint? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return BaseEquals(other) && Equals(Queue, other.Queue);
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

            return Equals((RabbitQueueProducerEndpoint)obj);
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode", Justification = "Protected set is not abused")]
        public override int GetHashCode() => Name.GetHashCode(StringComparison.Ordinal);
    }
}
