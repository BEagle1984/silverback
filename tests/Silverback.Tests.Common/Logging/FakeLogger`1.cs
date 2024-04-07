// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;

namespace Silverback.Tests.Logging;

public class FakeLogger<T> : ILogger<T>
{
    private readonly ILogger<T> _logger;

    public FakeLogger(ILoggerFactory factory)
    {
        _logger = factory.CreateLogger<T>();
    }

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter) => _logger.Log(
        logLevel,
        eventId,
        state,
        exception,
        formatter);

    public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => _logger.BeginScope(state);
}
