// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Tests.Logging;

namespace Silverback.Tests.Types;

public class SilverbackLoggerSubstitute<TCategory> : ISilverbackLogger<TCategory>
{
    private readonly LoggerSubstitute<TCategory> _logger = new(LogLevel.Trace);

    public ILogger InnerLogger => _logger;

    public bool IsEnabled(LogEvent logEvent) => _logger.IsEnabled(logEvent.Level);

    public void Received(
        LogLevel logLevel,
        Type? exceptionType,
        string? message = null,
        int? eventId = null,
        string? exceptionMessage = null) =>
        _logger.Received(logLevel, exceptionType, message, eventId, exceptionMessage);

    public bool DidNotReceive(
        LogLevel logLevel,
        Type? exceptionType,
        string? message = null,
        int? eventId = null,
        string? exceptionMessage = null) =>
        _logger.DidNotReceive(logLevel, exceptionType, message, eventId, exceptionMessage);
}
