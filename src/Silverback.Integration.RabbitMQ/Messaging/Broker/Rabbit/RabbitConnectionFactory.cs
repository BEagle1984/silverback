// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using RabbitMQ.Client;
using Silverback.Messaging.Configuration.Rabbit;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Rabbit
{
    /// <inheritdoc cref="IRabbitConnectionFactory" />
    public sealed class RabbitConnectionFactory : IRabbitConnectionFactory
    {
        private Dictionary<RabbitConnectionConfig, IConnection>? _connections = new();

        /// <inheritdoc cref="IRabbitConnectionFactory.GetChannel(RabbitProducerEndpoint, string)" />
        public IModel GetChannel(RabbitProducerEndpoint endpoint, string actualEndpointName)
        {
            Check.NotNull(endpoint, nameof(endpoint));

            var channel = GetConnection(endpoint.Connection).CreateModel();

            if (endpoint.ConfirmationTimeout.HasValue)
                channel.ConfirmSelect();

            switch (endpoint)
            {
                case RabbitQueueProducerEndpoint queueEndpoint:
                    channel.QueueDeclare(
                        actualEndpointName,
                        queueEndpoint.Queue.IsDurable,
                        queueEndpoint.Queue.IsExclusive,
                        queueEndpoint.Queue.IsAutoDeleteEnabled,
                        queueEndpoint.Queue.Arguments);
                    break;
                case RabbitExchangeProducerEndpoint exchangeEndpoint:
                    channel.ExchangeDeclare(
                        actualEndpointName,
                        exchangeEndpoint.Exchange.ExchangeType,
                        exchangeEndpoint.Exchange.IsDurable,
                        exchangeEndpoint.Exchange.IsAutoDeleteEnabled,
                        exchangeEndpoint.Exchange.Arguments);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(endpoint), "Unexpected endpoint type.");
            }

            return channel;
        }

        /// <inheritdoc cref="IRabbitConnectionFactory.GetChannel(RabbitConsumerEndpoint)" />
        public (IModel Channel, string QueueName) GetChannel(RabbitConsumerEndpoint endpoint)
        {
            Check.NotNull(endpoint, nameof(endpoint));

            var channel = GetConnection(endpoint.Connection).CreateModel();
            string queueName;

            switch (endpoint)
            {
                case RabbitQueueConsumerEndpoint queueEndpoint:
                    queueName = channel.QueueDeclare(
                            queueEndpoint.Name,
                            queueEndpoint.Queue.IsDurable,
                            queueEndpoint.Queue.IsExclusive,
                            queueEndpoint.Queue.IsAutoDeleteEnabled,
                            queueEndpoint.Queue.Arguments)
                        .QueueName;
                    break;
                case RabbitExchangeConsumerEndpoint exchangeEndpoint:
                    channel.ExchangeDeclare(
                        exchangeEndpoint.Name,
                        exchangeEndpoint.Exchange.ExchangeType,
                        exchangeEndpoint.Exchange.IsDurable,
                        exchangeEndpoint.Exchange.IsAutoDeleteEnabled,
                        exchangeEndpoint.Exchange.Arguments);

                    queueName = channel.QueueDeclare(
                            exchangeEndpoint.QueueName,
                            exchangeEndpoint.Queue.IsDurable,
                            exchangeEndpoint.Queue.IsExclusive,
                            exchangeEndpoint.Queue.IsAutoDeleteEnabled,
                            exchangeEndpoint.Queue.Arguments)
                        .QueueName;

                    channel.QueueBind(
                        queueName ?? string.Empty,
                        exchangeEndpoint.Name,
                        exchangeEndpoint.RoutingKey ?? string.Empty);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(endpoint), "Unexpected endpoint type.");
            }

            channel.BasicQos(endpoint.PrefetchSize, endpoint.PrefetchCount, false);

            return (channel, queueName!);
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            _connections?.Values.ForEach(connection => connection.Dispose());
            _connections = null;
        }

        private static IConnection CreateConnection(RabbitConnectionConfig connectionConfig)
        {
            var factory = new ConnectionFactory
            {
                DispatchConsumersAsync = true
            };

            factory.ApplyConfiguration(connectionConfig);

            return factory.CreateConnection();
        }

        private IConnection GetConnection(RabbitConnectionConfig connectionConfig)
        {
            Check.NotNull(connectionConfig, nameof(connectionConfig));

            if (_connections == null)
                throw new ObjectDisposedException(GetType().FullName);

            lock (_connections)
            {
                if (_connections.TryGetValue(connectionConfig, out IConnection connection))
                    return connection;

                connection = CreateConnection(connectionConfig);
                _connections.Add(connectionConfig, connection);

                return connection;
            }
        }
    }
}
