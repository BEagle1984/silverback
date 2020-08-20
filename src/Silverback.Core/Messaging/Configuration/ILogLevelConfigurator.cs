// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Configure the loglevels that should be used to log events.
    /// </summary>
    public interface ILogLevelConfigurator
    {
        /// <summary>
        ///     Configure the log level that should be applied to the specified event.
        /// </summary>
        /// <param name="eventId">
        ///     The event id.
        /// </param>
        /// <param name="logLevel">
        ///     The log level to apply.
        /// </param>
        /// <returns>
        ///     The <see cref="ILogLevelConfigurator" /> so that additional calls can be chained.
        /// </returns>
        ILogLevelConfigurator SetLogLevel(EventId eventId, LogLevel logLevel);

        /// <summary>
        ///     Configure a delegate that determines the log level that should be applied to the specified event.
        /// </summary>
        /// <param name="eventId">
        ///     The event id.
        /// </param>
        /// <param name="logLevelFunc">
        ///     The function that returns the log level. It takes the logged exception and the default log level as
        ///     parameters.
        /// </param>
        /// <returns>
        ///     The <see cref="ILogLevelConfigurator" /> so that additional calls can be chained.
        /// </returns>
        ILogLevelConfigurator SetLogLevel(EventId eventId, Func<Exception, LogLevel, LogLevel> logLevelFunc);

        /// <summary>
        ///     Configure a delegate that determines the log level that should be applied to the specified event.
        /// </summary>
        /// <param name="eventId">
        ///     The event id.
        /// </param>
        /// <param name="logLevelFunc">
        ///     The function that returns the log level. It takes the logged exception, the message and the default log level as
        ///     parameters.
        /// </param>
        /// <returns>
        ///     The <see cref="ILogLevelConfigurator" /> so that additional calls can be chained.
        /// </returns>
        ILogLevelConfigurator SetLogLevel(EventId eventId, Func<Exception, LogLevel, Lazy<string>, LogLevel> logLevelFunc);

        /// <summary>
        ///     Builds the <see cref="ILogLevelDictionary" /> based on the current state of the configurator.
        /// </summary>
        /// <returns>
        ///     The <see cref="ILogLevelDictionary" />.
        /// </returns>
        ILogLevelDictionary Build();
    }
}
