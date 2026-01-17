// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Configuration.Mqtt;

/// <summary>
///     Stores the mocked MQTT configuration.
/// </summary>
public interface IMockedMqttOptions
{
    /// <summary>
    ///     Gets or sets the delay to be applied before establishing a connection.
    /// </summary>
    TimeSpan ConnectionDelay { get; set; }
}
