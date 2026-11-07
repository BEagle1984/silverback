// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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
        Func<TState, Exception?, string> formatter)
    {
        if (_logLevelDictionary.TryGetValue(eventId, out LogLevelOverride? logLevelOverride))
            logLevel = GetMappedLogLevel(logLevelOverride.GetLogLevel, logLevel, state, exception, formatter);

        _innerLogger.Log(logLevel, eventId, state, exception, formatter);
    }

    // The event isn't known here and a mapped one could be uplifted, so the level can be trusted only without mappings
    public bool IsEnabled(LogLevel logLevel) => _logLevelDictionary.Count > 0 || _innerLogger.IsEnabled(logLevel);

    public bool IsEnabled(LogLevel logLevel, EventId eventId)
    {
        if (!_logLevelDictionary.TryGetValue(eventId, out LogLevelOverride? logLevelOverride))
            return _innerLogger.IsEnabled(logLevel);

        // A delegate can depend on the exception and the message, which are known only when logging
        return logLevelOverride.FixedLevel == null || _innerLogger.IsEnabled(logLevelOverride.FixedLevel.Value);
    }

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull =>
        _innerLogger.BeginScope(state);

    // Kept apart from Log because the closure is allocated on entry of the method declaring the captured parameters
    private static LogLevel GetMappedLogLevel<TState>(
        Func<Exception?, LogLevel, Lazy<string>, LogLevel> logLevelFunc,
        LogLevel logLevel,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        logLevelFunc(exception, logLevel, new Lazy<string>(() => formatter(state, exception)));
}
