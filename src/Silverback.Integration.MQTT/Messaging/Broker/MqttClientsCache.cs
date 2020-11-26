// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker.MqttNetWrappers;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="IMqttClientsCache" />
    internal sealed class MqttClientsCache : IMqttClientsCache
    {
        private readonly IMqttNetClientFactory _mqttClientFactory;

        private readonly Dictionary<MqttClientConfig, MqttClientWrapper> _clients = new();

        private readonly List<MqttClientWrapper> _extraClients = new();

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

            _extraClients.ForEach(clientWrapper => clientWrapper.Dispose());
            _extraClients.Clear();
        }

        private MqttClientWrapper GetClient(MqttClientConfig connectionConfig, bool forConsumer)
        {
            Check.NotNull(connectionConfig, nameof(connectionConfig));

            if (_clients == null)
                throw new ObjectDisposedException(null);

            lock (_clients)
            {
                bool clientExists = _clients.TryGetValue(connectionConfig, out MqttClientWrapper client);
                if (clientExists && (!forConsumer || client.Consumer == null))
                    return client;

                client = new MqttClientWrapper(_mqttClientFactory.CreateClient(), connectionConfig);

                if (!clientExists)
                    _clients.Add(connectionConfig, client);
                else
                    _extraClients.Add(client);

                return client;
            }
        }
    }
}
