// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Silverback.Tests.Logging
{
    public class LoggerSubstitute : ILogger
    {
        private readonly LoggerSubstituteFactory _factory;

        private readonly List<ReceivedCall> _receivedCalls = new();

        public LoggerSubstitute(ILoggerFactory factory)
        {
            _factory = (LoggerSubstituteFactory)factory;
        }

        public LogLevel MinLevel => _factory.MinLevel;

        public void Received(
            LogLevel logLevel,
            Type? exceptionType,
            string? message = null,
            int? eventId = null)
        {
            bool containsMatchingCall = _receivedCalls.Any(
                call =>
                    call.LogLevel == logLevel &&
                    call.ExceptionType == exceptionType
                    && (message == null || call.Message == message)
                    && (eventId == null || call.EventId == eventId));

            if (!containsMatchingCall)
            {
                var receivedCallsDump = string.Join(
                    ", ",
                    _receivedCalls.Select(
                        call =>
                            $"{call.LogLevel}, \"{call.Message}\", {call.ExceptionType?.Name ?? "no exception"}, {call.EventId}"));
                throw new InvalidOperationException(
                    $"No matching call received. Received calls: {receivedCallsDump}");
            }
        }

        public void Log<TState>(
            LogLevel logLevel,
            EventId eventId,
            TState state,
            Exception? exception,
            Func<TState, Exception?, string> formatter)
        {
            _receivedCalls.Add(
                new ReceivedCall(
                    logLevel,
                    exception?.GetType(),
                    formatter.Invoke(state, exception),
                    eventId.Id));
        }

        public bool IsEnabled(LogLevel logLevel) => logLevel >= _factory.MinLevel;

        public IDisposable BeginScope<TState>(TState state) => null!;

        private class ReceivedCall
        {
            public ReceivedCall(LogLevel logLevel, Type? exceptionType, string message, int eventId)
            {
                LogLevel = logLevel;
                ExceptionType = exceptionType;
                Message = message;
                EventId = eventId;
            }

            public LogLevel LogLevel { get; }

            public Type? ExceptionType { get; }

            public int EventId { get; }

            public string Message { get; }
        }
    }
}
