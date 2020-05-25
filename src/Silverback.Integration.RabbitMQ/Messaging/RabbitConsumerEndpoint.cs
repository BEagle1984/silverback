// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

namespace Silverback.Messaging
{
    /// <summary> Represents a queue or exchange to consume from. </summary>
    public abstract class RabbitConsumerEndpoint : ConsumerEndpoint
    {
        /// <summary> Initializes a new instance of the <see cref="RabbitConsumerEndpoint" /> class. </summary>
        /// <param name="name"> The name of the queue or exchange. </param>
        protected RabbitConsumerEndpoint(string name)
            : base(name)
        {
        }

        /// <summary> Gets or sets the RabbitMQ connection settings. </summary>
        public RabbitConnectionConfig Connection { get; set; } = new RabbitConnectionConfig();

        /// <summary> Gets or sets the queue configuration. </summary>
        public RabbitQueueConfig Queue { get; set; } = new RabbitQueueConfig();

        /// <summary>
        ///     Gets or sets the number of message to be processed before sending the acknowledgment to the server.
        ///     The most reliable level is 1 but it reduces throughput.
        /// </summary>
        public int AcknowledgeEach { get; set; } = 1;

        /// <summary> Gets or sets the QoS prefetch size parameter for the consumer. </summary>
        public uint PrefetchSize { get; set; }

        /// <summary> Gets or sets the QoS prefetch count parameter for the consumer. </summary>
        public ushort PrefetchCount { get; set; }

        /// <inheritdoc />
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

        /// <summary>
        ///     Determines whether the specified <see cref="RabbitConsumerEndpoint" /> is equal to the current
        ///     <see cref="RabbitConsumerEndpoint" />.
        /// </summary>
        /// <param name="other"> The object to compare with the current object. </param>
        /// <returns>
        ///     Returns a value indicating whether the other object is equal to the current object.
        /// </returns>
        protected bool Equals(RabbitConsumerEndpoint other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return base.Equals(other) && Equals(Connection, other.Connection) && Equals(Queue, other.Queue);
        }
    }
}
