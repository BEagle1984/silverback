// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using MQTTnet.Client;
using Silverback.Messaging.Broker.Mqtt.Mocks;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt
{
    /// <summary>
    ///     The factory used to create the <see cref="MockedMqttClient" /> instances.
    /// </summary>
    public class MockedMqttNetClientFactory : IMqttNetClientFactory
    {
        private readonly IInMemoryMqttBroker _broker;

        /// <summary>
        ///     Initializes a new instance of the <see cref="MockedMqttNetClientFactory" /> class.
        /// </summary>
        /// <param name="broker">
        ///     The <see cref="IInMemoryMqttBroker" />.
        /// </param>
        public MockedMqttNetClientFactory(IInMemoryMqttBroker broker)
        {
            _broker = Check.NotNull(broker, nameof(broker));
        }

        /// <inheritdoc cref="IMqttNetClientFactory.CreateClient" />
        public IMqttClient CreateClient() => new MockedMqttClient(_broker);
    }
}
