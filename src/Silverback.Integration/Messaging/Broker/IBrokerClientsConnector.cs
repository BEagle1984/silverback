// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Messaging.Broker;

/// <summary>
///     Initializes and connects the configured producers and consumers.
/// </summary>
internal interface IBrokerClientsConnector
{
    /// <summary>
    ///     Calls all the IBrokerClientsInitializer and initializes the producers and consumers.
    /// </summary>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    ValueTask InitializeAsync();

    /// <summary>
    ///     Calls all the IBrokerClientsInitializer and initializes the producers and consumers (if <see cref="InitializeAsync" /> wasn't
    ///     called yet), then initializes the connection to the message broker(s).
    /// </summary>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    ValueTask ConnectAllAsync(CancellationToken cancellationToken = default);

    /// <summary>
    ///     Disconnects all the producers and consumers.
    /// </summary>
    /// <returns>
    ///     A <see cref="ValueTask" /> representing the asynchronous operation.
    /// </returns>
    ValueTask DisconnectAllAsync();
}
