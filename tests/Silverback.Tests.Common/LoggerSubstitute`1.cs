// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Silverback.Tests
{
    public class LoggerSubstitute<T> : ILogger<T>
    {
        private readonly List<ReceivedCall> _receivedCalls = new List<ReceivedCall>();

        public void Received(LogLevel logLevel, Type? exceptionType)
        {
            if (!_receivedCalls.Any(call => call.LogLevel == logLevel && call.ExceptionType == exceptionType))
                throw new InvalidOperationException("No matching call received.");
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception exception,
            Func<TState, Exception, string> formatter)
        {
            _receivedCalls.Add(new ReceivedCall(logLevel, exception?.GetType()));
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable BeginScope<TState>(TState state)
        {
            return null!;
        }

        private class ReceivedCall
        {
            public ReceivedCall(LogLevel logLevel, Type? exceptionType)
            {
                LogLevel = logLevel;
                ExceptionType = exceptionType;
            }

            public LogLevel LogLevel { get; }

            public Type? ExceptionType { get; }
        }
    }
}
