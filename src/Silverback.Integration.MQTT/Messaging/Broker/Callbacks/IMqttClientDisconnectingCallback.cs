// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using MQTTnet.Client;
using Silverback.Messaging.Configuration.Mqtt;

namespace Silverback.Messaging.Broker.Callbacks;

/// <summary>
///     Declares the <see cref="OnClientDisconnectingAsync" /> event handler.
/// </summary>
public interface IMqttClientDisconnectingCallback : IBrokerClientCallback
{
    /// <summary>
    ///     Called when the underlying <see cref="IMqttClient" /> is disconnecting from the broker.
    /// </summary>
    /// <param name="configuration">
    ///     The client configuration.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task OnClientDisconnectingAsync(MqttClientConfiguration configuration);
}
