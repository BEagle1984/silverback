// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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
        /// Configure a handler function that calculates a loglevel based on the exception
        /// for the given event.
        /// </summary>
        /// <param name="eventId">The event id.</param>
        /// <param name="logLevelFunc">
        ///     The handler function that calculates the loglevel.
        ///     It takes the logged exception and the default log level as parameters.
        /// </param>
        /// <returns>The current <see cref="ILogLevelConfigurator"/>.</returns>
        ILogLevelConfigurator SetLogLevel(EventId eventId, Func<Exception, LogLevel, LogLevel> logLevelFunc);

        /// <summary>
        /// Build the <see cref="ILogLevelDictionary"/> based on the current state
        /// of the configurator.
        /// </summary>
        /// <returns>The <see cref="ILogLevelDictionary"/>.</returns>
        ILogLevelDictionary Build();
    }
}
