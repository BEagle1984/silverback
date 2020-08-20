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

        private readonly ILogLevelDictionary _logLevelDictionary;

        public SilverbackLogger(ILogger logger, ILogLevelDictionary logLevelDictionary)
        {
            _logger = logger;
            _logLevelDictionary = logLevelDictionary;
        }

        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            Func<Exception, LogLevel, Lazy<string>, LogLevel> logLevelFunc = _logLevelDictionary.GetValueOrDefault(eventId, (e, l, m) => logLevel);
            LogLevel logLevelToUse = logLevelFunc(exception, logLevel, new Lazy<string>(() => formatter(state, exception)));
            _logger.Log(logLevelToUse, eventId, state, exception, formatter);
        }
    }
}
