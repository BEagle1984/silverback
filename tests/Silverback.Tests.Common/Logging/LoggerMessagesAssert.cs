// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using System.Reflection;
using JetBrains.Annotations;
using Microsoft.Extensions.Logging;
using Shouldly;
using Silverback.Diagnostics;

namespace Silverback.Tests.Logging;

public static class LoggerMessagesAssert
{
    [AssertionMethod]
    public static void ShouldMatchLogEvents(Type loggerExtensionsType, Type logEventsType, params LogEvent[] handWrittenLogEvents)
    {
        (string Name, LogEvent LogEvent)[] logEvents =
        [
            .. logEventsType
                .GetProperties(BindingFlags.Public | BindingFlags.Static)
                .Select(property => (property.Name, (LogEvent)property.GetValue(null)!))
        ];
        (int Id, string? Name, LogLevel Level, string? Message)[] loggerMessages =
        [
            .. GetLoggerMessages(loggerExtensionsType)
                .Select(attribute => (attribute.EventId, attribute.EventName, attribute.Level, (string?)attribute.Message))
        ];

        logEvents.ShouldNotBeEmpty();
        logEvents.ShouldAllBe(logEvent => logEvent.LogEvent.EventId.Name!.EndsWith($"_{logEvent.Name}", StringComparison.Ordinal));
        loggerMessages.ShouldBe(
            logEvents
                .Select(logEvent => logEvent.LogEvent)
                .Except(handWrittenLogEvents)
                .Select(logEvent => (logEvent.EventId.Id, logEvent.EventId.Name, logEvent.Level, (string?)logEvent.Message)),
            ignoreOrder: true);
    }

    [AssertionMethod]
    public static void ShouldShareLogEvent(Type traceExtensionsType, LogEvent logEvent)
    {
        LoggerMessageAttribute[] loggerMessages = GetLoggerMessages(traceExtensionsType);

        loggerMessages.ShouldNotBeEmpty();
        loggerMessages.ShouldAllBe(attribute => attribute.EventId == logEvent.EventId.Id && attribute.Level == logEvent.Level);
        loggerMessages.Select(attribute => attribute.EventName).ShouldBeUnique();
    }

    private static LoggerMessageAttribute[] GetLoggerMessages(Type type) =>
    [
        .. type
            .GetMethods(BindingFlags.NonPublic | BindingFlags.Static)
            .Select(method => method.GetCustomAttribute<LoggerMessageAttribute>())
            .OfType<LoggerMessageAttribute>()
    ];
}
