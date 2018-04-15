using System;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// The base class for both <see cref="Consumer"/> and <see cref="Producer"/>.
    /// </summary>
    public abstract class ConsumerProducerBase
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="ConsumerProducerBase"/> class.
        /// </summary>
        /// <param name="broker">The broker.</param>
        /// <param name="endpoint">The endpoint.</param>
        protected ConsumerProducerBase(IBroker broker, IEndpoint endpoint)
        {
            Broker = broker ?? throw new ArgumentNullException(nameof(broker));
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));
            Serializer = broker.GetSerializer();
        }

        /// <summary>
        /// Gets the associated <see cref="IBroker"/>.
        /// </summary>
        public IBroker Broker { get; }

        /// <summary>
        /// Gets the endpoint this instance is connected to.
        /// </summary>
        public IEndpoint Endpoint { get; }

        /// <summary>
        /// Gets the message serializer to be used.
        /// </summary>
        public IMessageSerializer Serializer { get; }
    }
}