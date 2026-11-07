// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Logging;

namespace Silverback.Diagnostics;

/// <summary>
///     Contains the <see cref="LogEvent" /> constants of all events logged by the Silverback.Integration
///     package.
/// </summary>
// The other properties are generated from the [LoggerMessage] methods declared in IntegrationLoggerExtensions
public static partial class IntegrationLogEvents
{
    /// <summary>
    ///     Gets the <see cref="EventId" /> of the unspecific tracing logs.
    /// </summary>
    public static LogEvent Tracing { get; } = new(
        LogLevel.Trace,
        new EventId(1999, "Silverback.Integration_Tracing"),
        "The actual message will vary");
}
