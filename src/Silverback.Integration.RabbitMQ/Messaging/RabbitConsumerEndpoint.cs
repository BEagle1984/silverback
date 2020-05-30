// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

namespace Silverback.Messaging
{
    /// <summary>
    ///     Represents a queue or exchange to consume from.
    /// </summary>
    public abstract class RabbitConsumerEndpoint : ConsumerEndpoint
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="RabbitConsumerEndpoint" /> class.
        /// </summary>
        /// <param name="name">
        ///     The name of the queue or exchange.
        /// </param>
        protected RabbitConsumerEndpoint(string name)
            : base(name)
        {
        }

        /// <summary>
        ///     Gets or sets the RabbitMQ connection settings.
        /// </summary>
        public RabbitConnectionConfig Connection { get; set; } = new RabbitConnectionConfig();

        /// <summary>
        ///     Gets or sets the queue configuration.
        /// </summary>
        public RabbitQueueConfig Queue { get; set; } = new RabbitQueueConfig();

        /// <summary>
        ///     Gets or sets the number of message to be processed before sending the acknowledgment to the server.
        ///     The most reliable level is 1 but it reduces throughput.
        /// </summary>
        public int AcknowledgeEach { get; set; } = 1;

        /// <summary>
        ///     Gets or sets the QoS prefetch size parameter for the consumer.
        /// </summary>
        public uint PrefetchSize { get; set; }

        /// <summary>
        ///     Gets or sets the QoS prefetch count parameter for the consumer.
        /// </summary>
        public ushort PrefetchCount { get; set; }

        /// <inheritdoc cref="Endpoint.Validate" />
        public override void Validate()
        {
            base.Validate();

            if (Connection == null)
                throw new EndpointConfigurationException("Connection cannot be null");

            Connection.Validate();

            if (Queue == null)
                throw new EndpointConfigurationException("Queue cannot be null");

            Queue.Validate();

            if (AcknowledgeEach < 1)
                throw new EndpointConfigurationException("AcknowledgeEach cannot be less than 1");
        }

        /// <inheritdoc cref="Endpoint.BaseEquals" />
        protected override bool BaseEquals(Endpoint? other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (!(other is RabbitConsumerEndpoint otherRabbitConsumerEndpoint))
                return false;

            return base.BaseEquals(other) &&
                   Equals(Connection, otherRabbitConsumerEndpoint.Connection) &&
                   Equals(Queue, otherRabbitConsumerEndpoint.Queue);
        }
    }
}
