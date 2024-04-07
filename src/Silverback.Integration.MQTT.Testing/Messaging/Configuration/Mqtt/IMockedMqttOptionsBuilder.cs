// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     Exposes the methods to configure the mocked MQTT.
/// </summary>
public interface IMockedMqttOptionsBuilder
{
    /// <summary>
    ///     Specifies the delay to be applied before establishing a connection.
    /// </summary>
    /// <param name="delay">
    ///     The delay to be applied before establishing a connection.
    /// </param>
    /// <returns>
    ///     The <see cref="IMockedMqttOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    IMockedMqttOptionsBuilder DelayConnection(TimeSpan delay);
}
