// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;

namespace Silverback.Messaging.Configuration
{
    internal class LogLevelConfigurator : ILogLevelConfigurator
    {
        private readonly LogLevelMapping _logLevelMapping = new LogLevelMapping();

        public ILogLevelConfigurator SetLogLevel(EventId eventId, LogLevel logLevel)
        {
            _logLevelMapping[eventId] = logLevel;

            return this;
        }

        public ILogLevelMapping Build()
        {
            return _logLevelMapping;
        }
    }
}
