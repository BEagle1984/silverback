// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;

namespace Silverback.Messaging.Configuration
{
    internal class LogLevelConfigurator : ILogLevelConfigurator
    {
        private readonly LogLevelDictionary _logLevelDictionary = new();

        public ILogLevelConfigurator SetLogLevel(EventId eventId, LogLevel logLevel)
        {
            _logLevelDictionary[eventId] = (_, _, _) => logLevel;

            return this;
        }

        public ILogLevelConfigurator SetLogLevel(EventId eventId, Func<Exception, LogLevel, LogLevel> logLevelFunc)
        {
            _logLevelDictionary[eventId] =
                (exception, originalLogLevel, _) => logLevelFunc(exception, originalLogLevel);

            return this;
        }

        public ILogLevelConfigurator SetLogLevel(
            EventId eventId,
            Func<Exception, LogLevel, Lazy<string>, LogLevel> logLevelFunc)
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
