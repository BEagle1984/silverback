// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Can be used to configure the internal bus. It is typically used to setup the subscribers (if not
    ///     relying solely on the <see cref="ISubscriber" /> interface) and connect the message broker
    ///     (when using Silverback.Integration).
    /// </summary>
    public interface IBusConfigurator
    {
        /// <summary>
        ///     Gets the <see cref="BusOptions"/> to be configured.
        /// </summary>
        BusOptions BusOptions { get; }

        /// <summary>
        ///     Gets the related <see cref="IServiceProvider"/> to be used to resolve the required services.
        /// </summary>
        IServiceProvider ServiceProvider { get; }
    }
}
