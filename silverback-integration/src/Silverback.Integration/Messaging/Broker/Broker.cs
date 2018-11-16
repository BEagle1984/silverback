using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Serialization;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    public abstract class Broker : IBroker, IDisposable
    {
        private readonly ILogger<Broker> _logger;

        private ConcurrentDictionary<IEndpoint, Producer> _producersCache = new ConcurrentDictionary<IEndpoint, Producer>();
        private ConcurrentDictionary<IEndpoint, Consumer> _consumersCache = new ConcurrentDictionary<IEndpoint, Consumer>();

        protected readonly ILoggerFactory LoggerFactory;

        protected Broker(IMessageSerializer serializer, ILoggerFactory loggerFactory)
        {
            Serializer = serializer;
            LoggerFactory = loggerFactory;

            _logger = loggerFactory.CreateLogger<Broker>();
        }

        public IMessageSerializer Serializer { get; }

        #region Producer / Consumer

        public IProducer GetProducer(IEndpoint endpoint)
        {
            return _producersCache.GetOrAdd(endpoint, _ =>
            {
                _logger?.LogInformation($"Creating new producer for endpoint '{endpoint.Name}'");
                return InstantiateProducer(endpoint);
            });
        }

        protected abstract Producer InstantiateProducer(IEndpoint endpoint);

        public IConsumer GetConsumer(IEndpoint endpoint)
        {
            if (!IsConnected)
            {
                return _consumersCache.GetOrAdd(endpoint, _ =>
                {
                    _logger.LogInformation($"Creating new consumer for endpoint '{endpoint.Name}'");
                    return InstantiateConsumer(endpoint);
                });
            }

            if (!_consumersCache.TryGetValue(endpoint, out var consumer))
                throw new InvalidOperationException("The broker is already connected. Disconnect it to get a new consumer.");

            return consumer;
        }

        protected abstract Consumer InstantiateConsumer(IEndpoint endpoint);

        #endregion

        #region Connect / Disconnect

        public bool IsConnected { get; private set; }

        public void Connect()
        {
            _logger.LogTrace("Connecting to message broker...");

            Connect(_consumersCache.Values);
            IsConnected = true;

            _logger.LogTrace("Connected to message broker!");
        }

        protected abstract void Connect(IEnumerable<IConsumer> consumers);

        public void Disconnect()
        {
            _logger.LogTrace("Disconnecting from message broker...");

            Disconnect(_consumersCache.Values);
            IsConnected = false;

            _logger.LogTrace("Disconnected from message broker!");
        }

        protected abstract void Disconnect(IEnumerable<IConsumer> consumers);

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!disposing) return;

            _consumersCache?.Values.OfType<IDisposable>().ForEach(o => o.Dispose());
            _consumersCache = null;

            _producersCache?.Values.OfType<IDisposable>().ForEach(o => o.Dispose());
            _producersCache = null;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}