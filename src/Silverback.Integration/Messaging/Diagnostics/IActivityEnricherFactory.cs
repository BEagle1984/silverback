// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Diagnostics;

/// <summary>
///     Provides the <see cref="IBrokerActivityEnricher" /> according to the specified endpoint.
/// </summary>
public interface IActivityEnricherFactory
{
    /// <summary>
    ///     Returns the <see cref="IBrokerActivityEnricher" /> for the specified endpoint.
    /// </summary>
    /// <param name="endpointConfiguration">
    ///     The endpoint.
    /// </param>
    /// <returns>
    ///     The <see cref="IBrokerActivityEnricher" /> that matches the specified endpoint type.
    /// </returns>
    IBrokerActivityEnricher GetActivityEnricher(EndpointConfiguration endpointConfiguration);
}
