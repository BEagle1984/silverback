// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Logging;

namespace Silverback.Diagnostics;

/// <summary>
///     Used to perform logging in Silverback.
/// </summary>
public interface ISilverbackLogger
{
    /// <summary>
    ///     Gets the underlying <see cref="ILogger" />.
    /// </summary>
    ILogger InnerLogger { get; }

    /// <summary>
    ///     Checks if the given <see cref="LogEvent" /> is enabled according to its default or overridden
    ///     <see cref="LogLevel" />.
    /// </summary>
    /// <param name="logEvent">
    ///     The <see cref="LogEvent" /> to be checked.
    /// </param>
    /// <returns>
    ///     <c>true</c> if enabled.
    /// </returns>
    bool IsEnabled(LogEvent logEvent);
}
