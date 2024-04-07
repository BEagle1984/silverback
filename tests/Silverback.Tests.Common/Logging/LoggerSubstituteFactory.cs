// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Logging;

namespace Silverback.Tests.Logging;

public sealed class LoggerSubstituteFactory : ILoggerFactory
{
    public LoggerSubstituteFactory(LogLevel minLevel = LogLevel.Information)
    {
        MinLevel = minLevel;
    }

    public LogLevel MinLevel { get; }

    public ILogger CreateLogger(string categoryName) => new LoggerSubstitute(this);

    public void AddProvider(ILoggerProvider provider)
    {
    }

    public void Dispose()
    {
    }
}
