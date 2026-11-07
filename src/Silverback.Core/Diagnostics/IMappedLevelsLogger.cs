// Copyright (c) 2026 Sergio Aquilini
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
    ///     Checks if the given <see cref="EventId" /> is enabled according to its default or overridden <see cref="LogLevel" />. An
    ///     event whose <see cref="LogLevel" /> is determined by a delegate is always enabled, because the delegate is invoked when
    ///     the event is logged.
    /// </summary>
    /// <param name="logLevel">
    ///     Entry will be written on this level.
    /// </param>
    /// <param name="eventId">
    ///     Id of the event.
    /// </param>
    /// <returns>
    ///     <c>true</c> if enabled.
    /// </returns>
    bool IsEnabled(LogLevel logLevel, EventId eventId);
}
