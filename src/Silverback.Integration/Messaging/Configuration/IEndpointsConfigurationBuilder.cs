// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Exposes the methods to configure the inbound and outbound endpoints.
    /// </summary>
    public interface IEndpointsConfigurationBuilder
    {
        /// <summary>
        ///     Gets the <see cref="IServiceProvider" /> to be used to resolve the needed services.
        /// </summary>
        IServiceProvider ServiceProvider { get; }
    }
}
