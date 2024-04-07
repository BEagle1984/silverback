// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Logging;

namespace Silverback.Diagnostics;

/// <summary>
///     Wraps the <see cref="ILogger{TCategoryName}" /> mapping the log level according to the
///     <see cref="LogLevelDictionary" /> configuration.
/// </summary>
internal interface IMappedLevelsLogger : ILogger
{
    /// <summary>
    ///     Checks if the given <see cref="EventId" /> is enabled according to its default or overridden
    ///     <see cref="LogLevel" />.
    /// </summary>
    /// <param name="logLevel">
    ///     Entry will be written on this level.
    /// </param>
    /// <param name="eventId">
    ///     Id of the event.
    /// </param>
    /// <param name="message">
    ///     The raw message format string.
    /// </param>
    /// <returns>
    ///     <c>true</c> if enabled.
    /// </returns>
    bool IsEnabled(LogLevel logLevel, EventId eventId, string message);
}
