// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Logging;

namespace Silverback.Diagnostics;

/// <summary>
///     Encapsulates the log level, id and message.
/// </summary>
public class LogEvent
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="LogEvent" /> class.
    /// </summary>
    /// <param name="level">
    ///     The default <see cref="LogLevel" />.
    /// </param>
    /// <param name="eventId">
    ///     The <see cref="EventId" />.
    /// </param>
    /// <param name="message">
    ///     The logged message.
    /// </param>
    public LogEvent(LogLevel level, EventId eventId, string message)
    {
        Level = level;
        EventId = eventId;
        Message = message;
    }

    /// <summary>
    ///     Gets the default <see cref="LogLevel" />.
    /// </summary>
    public LogLevel Level { get; }

    /// <summary>
    ///     Gets the <see cref="EventId" />.
    /// </summary>
    public EventId EventId { get; }

    /// <summary>
    ///     Gets the logged message.
    /// </summary>
    public string Message { get; }
}
