// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Configuration;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     This interface can be implemented to split the producers and consumers configuration across different types. The types implementing
///     <see cref="IBrokerClientsConfigurator" /> must be registered using <see cref="SilverbackBuilderIntegrationExtensions.AddBrokerClientsConfigurator{TConfigurator}" />.
/// </summary>
public interface IBrokerClientsConfigurator
{
    /// <summary>
    ///     Configures the producers and consumers.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="BrokerClientsConfigurationBuilder" /> instance to be used to configure the producers and consumers.
    /// </param>
    void Configure(BrokerClientsConfigurationBuilder builder);
}
