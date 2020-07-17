// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;

namespace Silverback.Messaging.Configuration
{
    internal class LogLevelConfigurator : ILogLevelConfigurator
    {
        private readonly LogLevelDictionary _logLevelDictionary = new LogLevelDictionary();

        public ILogLevelConfigurator SetLogLevel(EventId eventId, LogLevel logLevel)
        {
            _logLevelDictionary[eventId] = (exception, originalLogLevel) => logLevel;

            return this;
        }

        public ILogLevelConfigurator SetLogLevel(EventId eventId, Func<Exception, LogLevel, LogLevel> logLevelFunc)
        {
            _logLevelDictionary[eventId] = logLevelFunc;

            return this;
        }

        public ILogLevelDictionary Build()
        {
            return _logLevelDictionary;
        }
    }
}
