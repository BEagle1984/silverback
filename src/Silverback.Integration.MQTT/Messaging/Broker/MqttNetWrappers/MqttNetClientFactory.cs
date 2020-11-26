// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using MQTTnet;
using MQTTnet.Client;

namespace Silverback.Messaging.Broker.MqttNetWrappers
{
    /// <inheritdoc cref="IMqttNetClientFactory" />
    public class MqttNetClientFactory : IMqttNetClientFactory
    {
        private readonly MqttFactory _factory = new();

        /// <inheritdoc cref="IMqttNetClientFactory.CreateClient" />
        public IMqttClient CreateClient() => _factory.CreateMqttClient();
    }
}
