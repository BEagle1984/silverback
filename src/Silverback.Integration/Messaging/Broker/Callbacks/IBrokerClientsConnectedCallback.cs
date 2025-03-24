// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Messaging.Broker.Callbacks;

/// <summary>
///     Declares the <see cref="OnBrokerClientsConnectedAsync" /> callback.
/// </summary>
public interface IBrokerClientsConnectedCallback : IBrokerClientCallback
{
    /// <summary>
    ///     Called when all clients have been configured and connected.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task OnBrokerClientsConnectedAsync();
}
