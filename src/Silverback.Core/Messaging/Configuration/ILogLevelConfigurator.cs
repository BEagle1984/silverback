// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    /// Configure the loglevels that should be used to log events.
    /// </summary>
    public interface ILogLevelConfigurator
    {
        /// <summary>
        /// Configure the loglevel that should be used to log a certain event.
        /// </summary>
        /// <param name="eventId">The event id.</param>
        /// <param name="logLevel">The loglevel to use.</param>
        /// <returns>The current <see cref="ILogLevelConfigurator"/>.</returns>
        ILogLevelConfigurator SetLogLevel(EventId eventId, LogLevel logLevel);

        /// <summary>
        /// Build the <see cref="ILogLevelMapping"/> based on the current state
        /// of the configurator.
        /// </summary>
        /// <returns>The <see cref="ILogLevelMapping"/>.</returns>
        ILogLevelMapping Build();
    }
}
