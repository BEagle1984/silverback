// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;

namespace Silverback.Tests.Logging;

public class FakeLogger : ILogger
{
    private readonly FakeLoggerFactory _factory;

    public FakeLogger(ILoggerFactory factory)
    {
        _factory = (FakeLoggerFactory)factory;
    }

    public LogLevel MinLevel => _factory.MinLevel;

    public IDisposable BeginScope<TState>(TState state)
        where TState : notnull => FakeLoggerScope.Instance;

    public bool IsEnabled(LogLevel logLevel) => logLevel >= MinLevel;

    public void Log<TState>(
        LogLevel logLevel,
        EventId eventId,
        TState state,
        Exception? exception,
        Func<TState, Exception?, string> formatter)
    {
        // Do nothing
    }
}
