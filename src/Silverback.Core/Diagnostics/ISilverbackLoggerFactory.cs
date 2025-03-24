// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Diagnostics;

/// <summary>
///     Creates instances of <see cref="ISilverbackLogger{TCategoryName}" />.
/// </summary>
public interface ISilverbackLoggerFactory
{
    /// <summary>
    ///     Creates a new <see cref="ISilverbackLogger{TCategoryName}" /> instance.
    /// </summary>
    /// <typeparam name="TCategoryName">
    ///     The category for messages produced by the logger.
    /// </typeparam>
    /// <returns>
    ///     The <see cref="ISilverbackLogger{TCategoryName}" />.
    /// </returns>
    ISilverbackLogger<TCategoryName> CreateLogger<TCategoryName>();
}
