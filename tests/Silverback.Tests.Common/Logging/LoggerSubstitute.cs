// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;

namespace Silverback.Tests.Logging;

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
        int? eventId = null,
        string? exceptionMessage = null)
    {
        bool containsMatchingCall = ContainsMatchingCall(
            logLevel,
            exceptionType,
            message,
            eventId,
            exceptionMessage);

        if (!containsMatchingCall)
        {
            string receivedCallsDump = string.Join(
                ", ",
                _receivedCalls.Select(
                    call =>
                        $"\r\n* {call.LogLevel}, \"{call.Message}\", {call.Exception?.GetType().Name ?? "no exception"}, {call.EventId}"));
            throw new InvalidOperationException($"No matching call received. Received calls: {receivedCallsDump}");
        }
    }

    public bool DidNotReceive(
        LogLevel logLevel,
        Type? exceptionType,
        string? message = null,
        int? eventId = null,
        string? exceptionMessage = null) =>
        !ContainsMatchingCall(
            logLevel,
            exceptionType,
            message,
            eventId,
            exceptionMessage);

    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter) =>
        _receivedCalls.Add(new ReceivedCall(logLevel, exception, formatter.Invoke(state, exception), eventId.Id));

    public bool IsEnabled(LogLevel logLevel) => logLevel >= _factory.MinLevel;

    public IDisposable? BeginScope<TState>(TState state)
        where TState : notnull => null;

    private bool ContainsMatchingCall(
        LogLevel logLevel,
        Type? exceptionType,
        string? message,
        int? eventId,
        string? exceptionMessage) =>
        _receivedCalls.Any(
            call =>
                call.LogLevel == logLevel &&
                call.Exception?.GetType() == exceptionType
                && (message == null || call.Message == message)
                && (eventId == null || call.EventId == eventId)
                && (exceptionMessage == null || call.Exception?.Message == exceptionMessage));

    private sealed class ReceivedCall
    {
        public ReceivedCall(LogLevel logLevel, Exception? exception, string message, int eventId)
        {
            LogLevel = logLevel;
            Exception = exception;
            Message = message;
            EventId = eventId;
        }

        public LogLevel LogLevel { get; }

        public Exception? Exception { get; }

        public int EventId { get; }

        public string Message { get; }
    }
}
