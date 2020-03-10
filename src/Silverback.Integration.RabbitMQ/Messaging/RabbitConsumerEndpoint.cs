// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

namespace Silverback.Messaging
{
    public abstract class RabbitConsumerEndpoint : ConsumerEndpoint
    {
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
        ///     Defines the number of message processed before sending the acknowledgment to the server.
        ///     The most reliable level is 1 but it reduces throughput.
        /// </summary>
        public int AcknowledgeEach { get; set; } = 1;
        
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

        #region Equality

        protected bool Equals(RabbitConsumerEndpoint other) =>
            base.Equals(other) && Equals(Connection, other.Connection) && Equals(Queue, other.Queue);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((RabbitConsumerEndpoint) obj);
        }

        #endregion
    }
}