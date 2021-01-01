// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Logging;

namespace Silverback.Tests.Logging
{
    public sealed class FakeLoggerFactory : ILoggerFactory
    {
        public FakeLoggerFactory(LogLevel minLevel = LogLevel.Information)
        {
            MinLevel = minLevel;
        }

        public LogLevel MinLevel { get; }

        public ILogger CreateLogger(string categoryName) => new FakeLogger(this);

        public void AddProvider(ILoggerProvider provider)
        {
        }

        public void Dispose()
        {
        }
    }
}
