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

        public void Received(LogLevel logLevel, Type? exceptionType, string? message = null)
        {
            bool containsMatchingCall = _receivedCalls.Any(
                call =>
                    call.LogLevel == logLevel &&
                    call.ExceptionType == exceptionType
                    && (message == null || call.Message == message));

            if (!containsMatchingCall)
                throw new InvalidOperationException("No matching call received.");
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _receivedCalls.Add(new ReceivedCall(logLevel, exception?.GetType(), formatter.Invoke(state, exception)));
        }

        public bool IsEnabled(LogLevel logLevel) => true;

        public IDisposable BeginScope<TState>(TState state)
        {
            return null!;
        }

        private class ReceivedCall
        {
            public ReceivedCall(LogLevel logLevel, Type? exceptionType, string message)
            {
                LogLevel = logLevel;
                ExceptionType = exceptionType;
                Message = message;
            }

            public LogLevel LogLevel { get; }

            public Type? ExceptionType { get; }

            public string Message { get; set; }
        }
    }
}
