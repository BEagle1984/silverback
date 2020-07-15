// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Silverback.Diagnostics
{

    internal class SilverbackLogger : ISilverbackLogger
    {
        private readonly ILogger _logger;
        private readonly ILogLevelMapping _logLevelMapping;

        public SilverbackLogger(ILogger logger, ILogLevelMapping logLevelMapping)
        {
            _logger = logger;
            _logLevelMapping = logLevelMapping;
        }

        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception exception, Func<TState, Exception, string> formatter) =>
            _logger.Log(_logLevelMapping.GetValueOrDefault(eventId, logLevel), eventId, state, exception, formatter);
    }
}
