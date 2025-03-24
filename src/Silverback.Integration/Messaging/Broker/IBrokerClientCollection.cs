// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Messaging.Broker;

/// <summary>
///     Holds a reference to all the configured <see cref="IBrokerClient" />.
/// </summary>
public interface IBrokerClientCollection : IReadOnlyCollection<IBrokerClient>
{
    /// <summary>
    ///     Gets the client with the specified name.
    /// </summary>
    /// <param name="name">
    ///     The client name.
    /// </param>
    /// <returns>
    ///     The <see cref="IBrokerClient" /> with the specified name.
    /// </returns>
    IBrokerClient this[string name] { get; }

    /// <summary>
    ///     Connects all the clients.
    /// </summary>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    ValueTask ConnectAllAsync();

    /// <summary>
    ///     Disconnects all the clients.
    /// </summary>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    ValueTask DisconnectAllAsync();
}
