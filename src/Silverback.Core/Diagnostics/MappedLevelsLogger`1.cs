// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Silverback.Util;

namespace Silverback.Diagnostics;

internal sealed class MappedLevelsLogger<TCategoryName> : IMappedLevelsLogger<TCategoryName>
{
    private readonly LogLevelDictionary _logLevelDictionary;

    private readonly ILogger _innerLogger;

    [SuppressMessage("ReSharper", "ContextualLoggerProblem", Justification = "It's correct to use ILogger<TCategoryName> here")]
    public MappedLevelsLogger(LogLevelDictionary logLevelDictionary, ILogger<TCategoryName> logger)
    {
        Check.NotNull(logger, nameof(logger));
        Check.NotNull(logLevelDictionary, nameof(logLevelDictionary));

        _innerLogger = logger;
        _logLevelDictionary = logLevelDictionary;
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        _innerLogger.Log(
            GetActualLogLevel(
                logLevel,
                eventId,
                exception,
                new Lazy<string>(() => formatter(state, exception))),
            eventId,
            state,
            exception,
            formatter);

    // Always return true to prevent uplifted logs to be prematurely ignored
    public bool IsEnabled(LogLevel logLevel) => true;

    public bool IsEnabled(LogLevel logLevel, EventId eventId, string message) =>
        _innerLogger.IsEnabled(
            GetActualLogLevel(
                logLevel,
                eventId,
                null,
                new Lazy<string>(message)));

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull =>
        _innerLogger.BeginScope(state);

    private LogLevel GetActualLogLevel(
        LogLevel logLevel,
        EventId eventId,
        Exception? exception,
        Lazy<string> lazyMessage)
    {
        Func<Exception?, LogLevel, Lazy<string>, LogLevel> logLevelFunc =
            _logLevelDictionary.GetValueOrDefault(eventId, (_, _, _) => logLevel);

        return logLevelFunc(exception, logLevel, lazyMessage);
    }
}
