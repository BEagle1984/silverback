// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Silverback.Diagnostics
{
    internal class SilverbackLogger<TCategoryName> : ISilverbackLogger<TCategoryName>
    {
        private readonly ILogger<TCategoryName> _logger;

        private readonly ILogLevelDictionary _logLevelDictionary;

        public SilverbackLogger(ILogger<TCategoryName> logger, ILogLevelDictionary loglevelDictionary)
        {
            _logger = logger;
            _logLevelDictionary = loglevelDictionary;
        }

        public IDisposable BeginScope<TState>(TState state) => _logger.BeginScope(state);

        public bool IsEnabled(LogLevel logLevel) => _logger.IsEnabled(logLevel);

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter) =>
            _logger.Log(
                _logLevelDictionary.GetValueOrDefault(eventId, (e, l) => logLevel)(exception, logLevel),
                eventId,
                state,
                exception,
                formatter);
    }
}
