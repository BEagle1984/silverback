// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Messaging.Broker.Callbacks;

/// <summary>
///     Declares the <see cref="OnEndpointsConfiguredAsync" /> callback.
/// </summary>
public interface IEndpointsConfiguredCallback : IBrokerCallback
{
    /// <summary>
    ///     Called when all endpoints have been configured.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task OnEndpointsConfiguredAsync();
}
