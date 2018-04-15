using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Broker
{
    // TODO: Review fluent implementation
    /// <summary>
    /// The base class for the <see cref="IBroker"/> implementation.
    /// </summary>
    /// <seealso cref="IBroker" />
    public abstract class Broker : IBroker
    {
        private Type _serializerType;
        private IMessageSerializer _serializer;

        /// <summary>
        /// Gets the configuration name. This is necessary only if working with multiple
        /// brokers.
        /// </summary>
        public string Name { get; private set; }
        
        /// <summary>
        /// Gets a value indicating whether this configuration is the default one.
        /// </summary>
        public bool IsDefault { get; private set; }

        /// <summary>
        /// Gets the <see cref="T:Silverback.Messaging.Serialization.IMessageSerializer" /> to be used to serialize and deserialize the messages
        /// being sent through the broker.
        /// </summary>
        /// <returns></returns>
        public virtual IMessageSerializer GetSerializer()
        {
            return _serializer ??
                   (_serializer = _serializerType != null
                       ? (IMessageSerializer) Activator.CreateInstance(_serializerType)
                       : new JsonMessageSerializer());
        }

        /// <summary>
        /// Gets a new <see cref="T:Silverback.Messaging.Broker.IProducer" /> instance.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns></returns>
        public abstract IProducer GetProducer(IEndpoint endpoint);

        /// <summary>
        /// Gets a new <see cref="T:Silverback.Messaging.Broker.IConsumer" /> instance.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns></returns>
        public abstract IConsumer GetConsumer(IEndpoint endpoint);

        /// <summary>
        /// Initializes the connection to the specified endpoints.
        /// </summary>
        /// <param name="inboundEndpoints">The inbound endpoints.</param>
        /// <param name="outboundEndpoints">The outbound endpoints.</param>
        public abstract void Connect(IEndpoint[] inboundEndpoints, IEndpoint[] outboundEndpoints);

        #region Configuration

        /// <summary>
        /// Set a name to the configuration. Necessary if working with multiple
        /// brokers.
        /// </summary>
        /// <param name="name">The name.</param>
        /// <returns></returns>
        public Broker WithName(string name)
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentNullException(nameof(name));

            Name = name;
            return this;
        }

        /// <summary>
        /// Set this as the default configuration.
        /// </summary>
        /// <returns></returns>
        public Broker AsDefault()
        {
            IsDefault = true;
            return this;
        }

        /// <summary>
        /// Set the <see cref="IMessageSerializer"/> to be used to serialize/deserialize the messages
        /// sent through the message broker.
        /// </summary>
        /// <typeparam name="TSerializer">The type of the serializer.</typeparam>
        /// <returns></returns>
        public Broker SerializeUsing<TSerializer>()
            where TSerializer : IMessageSerializer, new()
        {
            _serializerType = typeof(TSerializer);
            return this;
        }

        /// <summary>
        /// Called after the fluent configuration is applied, should verify the consistency of the 
        /// configuration.
        /// </summary>
        /// <remarks>An exception must be thrown if the confgiuration is not conistent.</remarks>
        public virtual void ValidateConfiguration()
        {
        }

        #endregion
    }
}