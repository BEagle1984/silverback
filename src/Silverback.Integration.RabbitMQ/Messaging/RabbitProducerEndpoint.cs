// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

namespace Silverback.Messaging
{
    /// <summary>
    ///     Represents a queue or exchange to produce to.
    /// </summary>
    public abstract class RabbitProducerEndpoint : ProducerEndpoint
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RabbitProducerEndpoint" /> class.
        /// </summary>
        /// <param name="name">
        ///     The name of the queue or exchange.
        /// </param>
        protected RabbitProducerEndpoint(string name)
            : base(name)
        {
        }

        /// <summary>
        ///     Gets or sets the RabbitMQ connection settings.
        /// </summary>
        public RabbitConnectionConfig Connection { get; set; } = new RabbitConnectionConfig();

        /// <summary>
        ///     Gets or sets the maximum amount of time to wait for the message produce to be acknowledge before
        ///     considering it failed. Set it to <c>null</c> to proceed without waiting for a positive or negative
        ///     acknowledgment (default is a quite
        ///     conservative 5 seconds).
        /// </summary>
        public TimeSpan? ConfirmationTimeout { get; set; } = TimeSpan.FromSeconds(5);

        /// <inheritdoc cref="ProducerEndpoint.Validate" />
        public override void Validate()
        {
            base.Validate();

            if (Connection == null)
                throw new EndpointConfigurationException("Connection cannot be null");

            Connection.Validate();
        }

        /// <inheritdoc cref="Endpoint.BaseEquals" />
        protected override bool BaseEquals(Endpoint? other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (!(other is RabbitProducerEndpoint otherRabbitProducerEndpoint))
                return false;

            return base.BaseEquals(other) && Equals(Connection, otherRabbitProducerEndpoint.Connection);
        }
    }
}
