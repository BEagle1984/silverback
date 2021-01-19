// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt
{
    /// <inheritdoc cref="IMqttClientsCache" />
    internal sealed class MqttClientsCache : IMqttClientsCache
    {
        private readonly IMqttNetClientFactory _mqttClientFactory;

        private readonly Dictionary<string, MqttClientWrapper> _clients = new();

        public MqttClientsCache(IMqttNetClientFactory mqttClientFactory)
        {
            _mqttClientFactory = mqttClientFactory;
        }

        public MqttClientWrapper GetClient(MqttProducer producer) =>
            GetClient(producer.Endpoint.Configuration, false);

        public MqttClientWrapper GetClient(MqttConsumer consumer)
        {
            var client = GetClient(consumer.Endpoint.Configuration, true);

            client.Consumer = consumer;

            return client;
        }

        public void Dispose()
        {
            _clients.Values.ForEach(clientWrapper => clientWrapper.Dispose());
            _clients.Clear();
        }

        [SuppressMessage(
            "ReSharper",
            "ParameterOnlyUsedForPreconditionCheck.Local",
            Justification = "Different checks for consumer")]
        private MqttClientWrapper GetClient(MqttClientConfig connectionConfig, bool isForConsumer)
        {
            Check.NotNull(connectionConfig, nameof(connectionConfig));

            if (_clients == null)
                throw new ObjectDisposedException(null);

            lock (_clients)
            {
                bool clientExists = _clients.TryGetValue(
                    connectionConfig.ClientId,
                    out MqttClientWrapper client);

                if (clientExists)
                {
                    if (!client.ClientConfig.Equals(connectionConfig))
                    {
                        throw new InvalidOperationException(
                            "A client with the same id is already connected but with a different configuration.");
                    }

                    if (isForConsumer && client.Consumer != null)
                    {
                        throw new InvalidOperationException(
                            "Cannot use the same client id for multiple consumers.");
                    }
                }
                else
                {
                    client = new MqttClientWrapper(_mqttClientFactory.CreateClient(), connectionConfig);
                    _clients.Add(connectionConfig.ClientId, client);
                }

                return client;
            }
        }
    }
}
