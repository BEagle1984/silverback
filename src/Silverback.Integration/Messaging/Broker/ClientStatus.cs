// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Broker;

/// <summary>
///     The possible states of an <see cref="IBrokerClient" />.
/// </summary>
public enum ClientStatus
{
    /// <summary>
    ///     The client is not connected and the connection hasn't been initialized yet.
    /// </summary>
    Disconnected = 0,

    /// <summary>
    ///     The <see cref="IBrokerClient.ConnectAsync" /> method has been called and the client is performing the initialization.
    /// </summary>
    Initializing = 1,

    /// <summary>
    ///     The client has been successfully initialized and the connection will eventually be established.
    /// </summary>
    Initialized = 2,

    /// <summary>
    ///     The <see cref="IBrokerClient.DisconnectAsync" /> method has been called and the client is disconnecting.
    /// </summary>
    Disconnecting = 3
}
