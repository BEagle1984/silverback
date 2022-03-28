// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;

namespace Silverback.Diagnostics;

/// <summary>
///     Builds an <see cref="IBrokerLogEnricher" /> instance for the <see cref="EndpointConfiguration" />.
/// </summary>
public interface IBrokerLogEnricherFactory
{
    /// <summary>
    ///     Returns an <see cref="IBrokerLogEnricher" /> according to the specified endpoint configuration.
    /// </summary>
    /// <param name="configuration">
    ///     The endpoint configuration that will be used to create the <see cref="IBrokerLogEnricher" />.
    /// </param>
    /// <returns>
    ///     The <see cref="IBrokerLogEnricher" />.
    /// </returns>
    IBrokerLogEnricher GetEnricher(EndpointConfiguration configuration);
}
