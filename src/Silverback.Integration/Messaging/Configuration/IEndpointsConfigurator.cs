// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     This interface can be implemented to split the message broker endpoints configuration across
    ///     different types. The types implementing <see cref="IEndpointsConfigurator" /> must be registered
    ///     using <c>RegisterConfigurator</c> or <c>AddEndpointConfigurator</c>.
    /// </summary>
    public interface IEndpointsConfigurator
    {
        /// <summary>
        ///     Configures the message broker endpoints.
        /// </summary>
        /// <param name="builder">
        ///     The <see cref="IEndpointsConfigurationBuilder" /> instance to be used to configure the endpoints.
        /// </param>
        void Configure(IEndpointsConfigurationBuilder builder);
    }
}
