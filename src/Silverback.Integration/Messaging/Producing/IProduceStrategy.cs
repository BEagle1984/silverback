// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Producing;

/// <summary>
///     The strategy used to produce the messages.
/// </summary>
public interface IProduceStrategy : IEquatable<IProduceStrategy>
{
    /// <summary>
    ///     Returns the actual strategy implementation, built using the provided <see cref="IServiceProvider" />.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to build the strategy.
    /// </param>
    /// <param name="endpointConfiguration">
    ///     The producer endpoint configuration.
    /// </param>
    /// <returns>
    ///     An instance of <see cref="IProduceStrategyImplementation" /> that can be used to produce the messages.
    /// </returns>
    IProduceStrategyImplementation Build(IServiceProvider serviceProvider, ProducerEndpointConfiguration endpointConfiguration);
}
