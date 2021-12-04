// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Inbound.ExactlyOnce;

/// <summary>
///     The strategy used to guarantee that each message is consumed only once.
/// </summary>
public interface IExactlyOnceStrategy
{
    /// <summary>
    ///     Returns the actual strategy implementation, built using the provided <see cref="IServiceProvider" />.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to build the strategy.
    /// </param>
    /// <returns>
    ///     An instance of <see cref="IExactlyOnceStrategyImplementation" /> that can be used to produce the
    ///     messages.
    /// </returns>
    IExactlyOnceStrategyImplementation Build(IServiceProvider serviceProvider);
}
