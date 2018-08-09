using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Serialization;
using Silverback.Extensions;

namespace Silverback.Messaging.Broker
{
    // TODO: Review fluent implementation
    /// <summary>
    /// The base class for the <see cref="IBroker"/> implementation.
    /// </summary>
    /// <seealso cref="IBroker" />
    public abstract class Broker : IBroker, IDisposable
    {
        #region Ctor

        /// <summary>
        /// Initializes a new instance of the <see cref="Broker" /> class.
        /// </summary>
        protected Broker()
        {
            UseLogger(NullLoggerFactory.Instance);
        }

        #endregion

        #region Get Producers / Consumers / Serializer

        private ConcurrentDictionary<IEndpoint, Producer> _producers = new ConcurrentDictionary<IEndpoint, Producer>();
        private ConcurrentDictionary<IEndpoint, Consumer> _consumers = new ConcurrentDictionary<IEndpoint, Consumer>();

        /// <summary>
        /// Gets the <see cref="T:Silverback.Messaging.Serialization.IMessageSerializer" /> to be used to serialize and deserialize the messages
        /// being sent through the broker.
        /// </summary>
        /// <returns></returns>
        public virtual IMessageSerializer GetSerializer()
        {
            return _serializer ??
                   (_serializer = _serializerType != null
                       ? (IMessageSerializer)Activator.CreateInstance(_serializerType)
                       : new JsonMessageSerializer());
        }

        /// <summary>
        /// Gets an <see cref="T:Silverback.Messaging.Broker.IProducer" /> instance to publish messages to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns></returns>
        /// TODO: Throw exception if connected?
        public IProducer GetProducer(IEndpoint endpoint)
        {
            return _producers.GetOrAdd(endpoint, _ =>
            {
                _logger.LogInformation($"Creating new producer for endpoint '{endpoint.Name}'");
                return GetNewProducer(endpoint);
            });
        }

        /// <summary>
        /// Gets a new <see cref="Producer" /> instance to publish messages to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns></returns>
        public abstract Producer GetNewProducer(IEndpoint endpoint);

        /// <summary>
        /// Gets an <see cref="T:Silverback.Messaging.Broker.IConsumer" /> instance to listen to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns></returns>
        public IConsumer GetConsumer(IEndpoint endpoint)
        {
            if (!IsConnected)
            {
                return _consumers.GetOrAdd(endpoint, _ =>
                {
                    _logger.LogInformation($"Creating new consumer for endpoint '{endpoint.Name}'");
                    return GetNewConsumer(endpoint);
                });
            }

            if (!_consumers.TryGetValue(endpoint, out var consumer))
                throw new InvalidOperationException("The broker is already connected. Disconnect it to get a new consumer.");

            return consumer;
        }

        /// <summary>
        /// Gets a value indicating whether this instance is connected with the message broker.
        /// </summary>
        /// <value>
        /// <c>true</c> if this instance is connected; otherwise, <c>false</c>.
        /// </value>
        public bool IsConnected { get; private set; }

        /// <summary>
        /// Gets a new <see cref="Consumer" /> instance to listen to the specified endpoint.
        /// </summary>
        /// <param name="endpoint">The endpoint.</param>
        /// <returns></returns>
        public abstract Consumer GetNewConsumer(IEndpoint endpoint);

        #endregion

        #region Connect / Disconnect

        /// <summary>
        /// Connects to the message broker.
        /// After a successful call to this method the created consumers will start listening to
        /// their endpoints and the producers will be ready to send messages.
        /// </summary>
        public void Connect()
        {
            _logger.LogTrace("Connecting...");

            RemoveDisposedObjects(_consumers);
            RemoveDisposedObjects(_producers);

            Connect(_consumers.Values);
            Connect(_producers.Values);

            IsConnected = true;

            _logger.LogTrace("Connected!");
        }

        /// <summary>
        /// Removes the disposed objects from the dictionary.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objects">The objects.</param>
        private void RemoveDisposedObjects<T>(IDictionary<IEndpoint, T> objects)
            where T : EndpointConnectedObject
        {
            objects.Where(c => c.Value.IsDisposed).ToList().ForEach(c => objects.Remove(c.Key));
        }

        /// <summary>
        /// Connects the specified consumers.
        /// After a successful call to this method the consumers will start listening to
        /// their endpoints.
        /// </summary>
        /// <param name="consumers">The consumers.</param>
        protected abstract void Connect(IEnumerable<IConsumer> consumers);

        /// <summary>
        /// Connects the specified producers.
        /// After a successful call to this method the producers will be ready to send messages.
        /// </summary>
        /// <param name="producers">The producers.</param>
        protected abstract void Connect(IEnumerable<IProducer> producers);

        /// <summary>
        /// Disconnects from the message broker. The related consumers will not receive any further
        /// message and the producers will not be able to send messages anymore.
        /// </summary>
        public void Disconnect()
        {
            _logger.LogTrace("Disconnecting...");

            RemoveDisposedObjects(_consumers);
            RemoveDisposedObjects(_producers);

            Disconnect(_consumers.Values);
            Disconnect(_producers.Values);

            IsConnected = false;

            _logger.LogTrace("Disconnected!");
        }

        /// <summary>
        /// Disconnects the specified consumers. The consumers will not receive any further
        /// message.
        /// their endpoints.
        /// </summary>
        /// <param name="consumers">The consumers.</param>
        protected abstract void Disconnect(IEnumerable<IConsumer> consumers);

        /// <summary>
        /// Disconnects the specified producers. The producers will not be able to send messages anymore.
        /// </summary>
        /// <param name="producer">The producer.</param>
        protected abstract void Disconnect(IEnumerable<IProducer> producer);

        #endregion

        #region Configuration

        private ILogger _logger;
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
        /// The logger factory
        /// </summary>
        public ILoggerFactory LoggerFactory { get; private set; }

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

        /// <summary>
        /// Configures the specified <see cref="ILoggerFactory" /> to be used within the bus.
        /// </summary>
        /// <param name="loggerFactory">The logger factory.</param>
        /// <returns></returns>
        public Broker UseLogger(ILoggerFactory loggerFactory)
        {
            LoggerFactory = loggerFactory;
            _logger = loggerFactory.CreateLogger<Broker>();

            return this;
        }

        #endregion

        #region IDisposable

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (disposing)
            {
                _consumers?.Values.Where(o => !o.IsDisposed).ForEach(o => o.Dispose());
                _consumers = null;

                _producers?.Values.Where(o => !o.IsDisposed).ForEach(o => o.Dispose());
                _producers = null;
            }
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}