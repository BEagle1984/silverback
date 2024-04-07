// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration.Mqtt;

namespace Silverback.Messaging.Broker.Mqtt;

/// <summary>
///     Creates <see cref="IMqttClientWrapperFactory" /> instances.
/// </summary>
// TODO: Test
public interface IMqttClientWrapperFactory
{
    /// <summary>
    ///     Creates a new <see cref="IMqttClientWrapper" />.
    /// </summary>
    /// <param name="name">
    ///     The name of the client.
    /// </param>
    /// <param name="configuration">
    ///     The <see cref="MqttClientConfiguration" />.
    /// </param>
    /// <returns>
    ///     The <see cref="IMqttClientWrapper" />.
    /// </returns>
    IMqttClientWrapper Create(string name, MqttClientConfiguration configuration);
}
