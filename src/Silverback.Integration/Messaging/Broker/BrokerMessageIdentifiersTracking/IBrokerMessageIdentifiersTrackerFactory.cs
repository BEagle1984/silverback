// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Broker.BrokerMessageIdentifiersTracking;

/// <summary>
///     Builds an <see cref="IBrokerMessageIdentifiersTracker" /> instance for the <see cref="EndpointConfiguration" />.
/// </summary>
public interface IBrokerMessageIdentifiersTrackerFactory
{
    /// <summary>
    ///     Returns the <see cref="IBrokerMessageIdentifiersTracker" /> compatible with the specific message broker.
    /// </summary>
    /// <param name="configuration">
    ///     The endpoint configuration that will be used to determine the <see cref="IBrokerMessageIdentifiersTracker" /> to be used.
    /// </param>
    /// <returns>
    ///     The <see cref="IBrokerMessageIdentifiersTracker" />.
    /// </returns>
    IBrokerMessageIdentifiersTracker GetTracker(EndpointConfiguration configuration);
}
