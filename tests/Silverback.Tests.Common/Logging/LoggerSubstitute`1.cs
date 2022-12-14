// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;

namespace Silverback.Tests.Logging;

public class LoggerSubstitute<T> : ILogger<T>
{
    private readonly LoggerSubstitute _logger;

    public LoggerSubstitute(ILoggerFactory factory)
    {
        _logger = new LoggerSubstitute(factory);
    }

    public LoggerSubstitute(LogLevel minLevel)
    {
        _logger = new LoggerSubstitute(new LoggerSubstituteFactory(minLevel));
    }

    public void Received(
        LogLevel logLevel,
        Type? exceptionType,
        string? message = null,
        int? eventId = null,
        string? exceptionMessage = null) =>
        _logger.Received(logLevel, exceptionType, message, eventId, exceptionMessage);

    public bool DidNotReceive(
        LogLevel logLevel,
        Type? exceptionType,
        string? message = null,
        int? eventId = null,
        string? exceptionMessage = null) =>
        _logger.DidNotReceive(logLevel, exceptionType, message, eventId, exceptionMessage);

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) =>
        _logger.Log(logLevel, eventId, state, exception, formatter);

    public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => _logger.BeginScope(state);
}
