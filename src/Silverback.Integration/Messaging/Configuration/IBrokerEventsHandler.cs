// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Exposes some event hooks that are being called during configuration and initialization
    ///     of brokers.
    /// </summary>
    public interface IBrokerEventsHandler
    {
        /// <summary>
        ///     Is triggered when all endpoints were configured.
        /// </summary>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        Task OnEndpointsConfiguredAsync();
    }
}
