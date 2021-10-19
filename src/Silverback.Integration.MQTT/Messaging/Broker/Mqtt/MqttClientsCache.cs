// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt
{
    /// <inheritdoc cref="IMqttClientsCache" />
    internal sealed class MqttClientsCache : IMqttClientsCache
    {
        private readonly IMqttNetClientFactory _mqttClientFactory;

        private readonly IBrokerCallbacksInvoker _callbacksInvoker;

        private readonly ISilverbackLogger _logger;

        private readonly Dictionary<string, MqttClientWrapper> _clients = new();

        public MqttClientsCache(
            IMqttNetClientFactory mqttClientFactory,
            IBrokerCallbacksInvoker callbacksInvoker,
            ISilverbackLogger<MqttClientsCache> logger)
        {
            _mqttClientFactory = Check.NotNull(mqttClientFactory, nameof(mqttClientFactory));
            _callbacksInvoker = Check.NotNull(callbacksInvoker, nameof(callbacksInvoker));
            _logger = Check.NotNull(logger, nameof(logger));
        }

        public MqttClientWrapper GetClient(MqttProducer producer) =>
            GetClient(producer.Configuration.Client, false);

        public MqttClientWrapper GetClient(MqttConsumer consumer)
        {
            var client = GetClient(consumer.Configuration.Client, true);

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
        private MqttClientWrapper GetClient(MqttClientConfiguration clientConfiguration, bool isForConsumer)
        {
            Check.NotNull(clientConfiguration, nameof(clientConfiguration));

            if (_clients == null)
                throw new ObjectDisposedException(GetType().FullName);

            lock (_clients)
            {
                bool clientExists = _clients.TryGetValue(
                    clientConfiguration.ClientId,
                    out MqttClientWrapper client);

                if (clientExists)
                {
                    if (!client.ClientConfiguration.Equals(clientConfiguration))
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
                    client = new MqttClientWrapper(
                        _mqttClientFactory.CreateClient(),
                        clientConfiguration,
                        _callbacksInvoker,
                        _logger);
                    _clients.Add(clientConfiguration.ClientId, client);
                }

                return client;
            }
        }
    }
}
