// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using MQTTnet;
using Silverback.Messaging.Broker.Mqtt.Mocks;
using Silverback.Messaging.Configuration.Mqtt;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Mqtt;

/// <summary>
///     The factory used to create the <see cref="MockedMqttClient" /> instances.
/// </summary>
public class MockedMqttNetClientFactory : IMqttNetClientFactory
{
    private readonly IMockedMqttOptions _options;

    private readonly IInMemoryMqttBroker _broker;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MockedMqttNetClientFactory" /> class.
    /// </summary>
    /// <param name="broker">
    ///     The <see cref="IInMemoryMqttBroker" />.
    /// </param>
    /// <param name="options">
    ///     The <see cref="IMockedMqttOptions" />.
    /// </param>
    public MockedMqttNetClientFactory(IInMemoryMqttBroker broker, IMockedMqttOptions options)
    {
        _broker = Check.NotNull(broker, nameof(broker));
        _options = Check.NotNull(options, nameof(options));
    }

    /// <inheritdoc cref="IMqttNetClientFactory.CreateClient" />
    public IMqttClient CreateClient() => new MockedMqttClient(_broker, _options);
}
