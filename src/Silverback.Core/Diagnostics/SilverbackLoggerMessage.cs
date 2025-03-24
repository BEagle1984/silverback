// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;

namespace Silverback.Diagnostics;

internal static class SilverbackLoggerMessage
{
    public static Action<ILogger, Exception?> Define(LogEvent logEvent) =>
        LoggerMessage.Define(logEvent.Level, logEvent.EventId, logEvent.Message);

    public static Action<ILogger, T1, Exception?> Define<T1>(LogEvent logEvent) =>
        LoggerMessage.Define<T1>(logEvent.Level, logEvent.EventId, logEvent.Message);

    public static Action<ILogger, T1, T2, Exception?> Define<T1, T2>(LogEvent logEvent) =>
        LoggerMessage.Define<T1, T2>(logEvent.Level, logEvent.EventId, logEvent.Message);

    public static Action<ILogger, T1, T2, T3, Exception?> Define<T1, T2, T3>(LogEvent logEvent) =>
        LoggerMessage.Define<T1, T2, T3>(logEvent.Level, logEvent.EventId, logEvent.Message);

    public static Action<ILogger, T1, T2, T3, T4, Exception?> Define<T1, T2, T3, T4>(LogEvent logEvent) =>
        LoggerMessage.Define<T1, T2, T3, T4>(logEvent.Level, logEvent.EventId, logEvent.Message);

    public static Action<ILogger, T1, T2, T3, T4, T5, Exception?> Define<T1, T2, T3, T4, T5>(LogEvent logEvent) =>
        LoggerMessage.Define<T1, T2, T3, T4, T5>(logEvent.Level, logEvent.EventId, logEvent.Message);

    public static Action<ILogger, T1, T2, T3, T4, T5, T6, Exception?> Define<T1, T2, T3, T4, T5, T6>(LogEvent logEvent) =>
        LoggerMessage.Define<T1, T2, T3, T4, T5, T6>(logEvent.Level, logEvent.EventId, logEvent.Message);
}
