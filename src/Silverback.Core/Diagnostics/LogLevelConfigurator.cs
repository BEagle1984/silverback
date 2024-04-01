// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;

namespace Silverback.Diagnostics;

/// <summary>
///     Configure and stores the <see cref="LogLevel" /> overrides.
/// </summary>
public sealed class LogLevelConfigurator
{
    internal LogLevelDictionary LogLevelDictionary { get; } = [];

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
    ///     The <see cref="LogLevelConfigurator" /> so that additional calls can be chained.
    /// </returns>
    public LogLevelConfigurator SetLogLevel(EventId eventId, LogLevel logLevel)
    {
        LogLevelDictionary[eventId] = (_, _, _) => logLevel;
        return this;
    }

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
    ///     The <see cref="LogLevelConfigurator" /> so that additional calls can be chained.
    /// </returns>
    public LogLevelConfigurator SetLogLevel(EventId eventId, Func<Exception?, LogLevel, LogLevel> logLevelFunc)
    {
        LogLevelDictionary[eventId] = (exception, originalLogLevel, _) => logLevelFunc(exception, originalLogLevel);
        return this;
    }

    /// <summary>
    ///     Configure a delegate that determines the log level that should be applied to the specified event.
    /// </summary>
    /// <param name="eventId">
    ///     The event id.
    /// </param>
    /// <param name="logLevelFunc">
    ///     The function that returns the log level. It takes the logged exception, the message and the default
    ///     log level as
    ///     parameters.
    /// </param>
    /// <returns>
    ///     The <see cref="LogLevelConfigurator" /> so that additional calls can be chained.
    /// </returns>
    public LogLevelConfigurator SetLogLevel(EventId eventId, Func<Exception?, LogLevel, Lazy<string>, LogLevel> logLevelFunc)
    {
        LogLevelDictionary[eventId] = logLevelFunc;
        return this;
    }
}
