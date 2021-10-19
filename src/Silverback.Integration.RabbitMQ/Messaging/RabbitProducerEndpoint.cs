// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration.Rabbit;

namespace Silverback.Messaging
{
    /// <summary>
    ///     The Rabbit producer configuration.
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
        ///     Gets the RabbitMQ connection settings.
        /// </summary>
        public RabbitConnectionConfig Connection { get; init; } = new();

        /// <summary>
        ///     Gets the maximum amount of time to wait for the message produce to be acknowledge before
        ///     considering it failed. Set it to <c>null</c> to proceed without waiting for a positive or negative
        ///     acknowledgment. The default is a quite conservative 5 seconds.
        /// </summary>
        public TimeSpan? ConfirmationTimeout { get; init; } = TimeSpan.FromSeconds(5);

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

            if (other is not RabbitProducerEndpoint otherRabbitProducerEndpoint)
                return false;

            return base.BaseEquals(other) && Equals(Connection, otherRabbitProducerEndpoint.Connection);
        }
    }
}
