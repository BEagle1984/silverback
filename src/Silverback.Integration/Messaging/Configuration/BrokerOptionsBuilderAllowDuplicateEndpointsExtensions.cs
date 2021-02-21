// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>AllowDuplicateEndpointRegistrations</c> method to the <see cref="IBrokerOptionsBuilder"/>.
    /// </summary>
    public static class BrokerOptionsBuilderAllowDuplicateEndpointsExtensions
    {
        /// <summary>
        ///     Enables registration of duplicate endpoints.
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder"/>.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder"/> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AllowDuplicateEndpointRegistrations(this IBrokerOptionsBuilder brokerOptionsBuilder)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            var outboundRoutingConfiguration =
                brokerOptionsBuilder.SilverbackBuilder.Services.GetSingletonServiceInstance<IOutboundRoutingConfiguration>() ??
                throw new InvalidOperationException(
                    "IOutboundRoutingConfiguration not found, " +
                    "WithConnectionToMessageBroker has not been called.");

            outboundRoutingConfiguration.IdempotentEndpointRegistration = false;

            return brokerOptionsBuilder;
        }
    }
}
