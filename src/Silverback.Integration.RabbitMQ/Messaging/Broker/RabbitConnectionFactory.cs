// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using RabbitMQ.Client;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    public class RabbitConnectionFactory : IDisposable
    {
        private ConcurrentDictionary<RabbitConnectionConfig, IConnection> _connections = new ConcurrentDictionary<RabbitConnectionConfig, IConnection>();
        
        public IModel GetChannel(RabbitProducerEndpoint endpoint)
        {
            var channel = GetConnection(endpoint.Connection).CreateModel();

            if (endpoint.ConfirmationTimeout.HasValue)
                channel.ConfirmSelect();

            switch (endpoint)
            {
                case RabbitQueueProducerEndpoint queueEndpoint:
                    channel.QueueDeclare(
                        queueEndpoint.Name,
                        queueEndpoint.Queue.IsDurable,
                        queueEndpoint.Queue.IsExclusive,
                        queueEndpoint.Queue.IsAutoDeleteEnabled,
                        queueEndpoint.Queue.Arguments);
                    break;
                case RabbitExchangeProducerEndpoint exchangeEndpoint:
                    channel.ExchangeDeclare(
                            exchangeEndpoint.Name,
                            exchangeEndpoint.Exchange.ExchangeType,
                            exchangeEndpoint.Exchange.IsDurable,
                            exchangeEndpoint.Exchange.IsAutoDeleteEnabled,
                            exchangeEndpoint.Exchange.Arguments);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
            
            return channel;
        }
        
        public IConnection GetConnection(RabbitConnectionConfig connectionConfig)
        {
            if (connectionConfig == null) throw new ArgumentNullException(nameof(connectionConfig));
            if (_connections == null) throw new ObjectDisposedException(null);

            return _connections.GetOrAdd(connectionConfig, _ => CreateConnection(connectionConfig));
        }

        private IConnection CreateConnection(RabbitConnectionConfig connectionConfig)
        {
            var factory = new ConnectionFactory();
            factory.ApplyConfiguration(connectionConfig);
            return factory.CreateConnection();
        }

        public void Dispose()
        {
            _connections?.ForEach(c => c.Value?.Dispose());
            _connections = null;
        }
    }
}