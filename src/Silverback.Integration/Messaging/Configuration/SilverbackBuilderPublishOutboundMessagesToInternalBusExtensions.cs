// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Connectors;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Adds the <c>PublishOutboundMessagesToInternalBus</c> method to the <see cref="ISilverbackBuilder" />.
    /// </summary>
    public static class SilverbackBuilderPublishOutboundMessagesToInternalBusExtensions
    {
        /// <summary>
        ///     Enables the legacy behavior where the messages to be routed through an outbound connector are also
        ///     being published to the internal bus, to be locally subscribed. This is now disabled by default.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" />.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder PublishOutboundMessagesToInternalBus(this ISilverbackBuilder silverbackBuilder)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            var outboundRoutingConfiguration =
                silverbackBuilder.Services.GetSingletonServiceInstance<IOutboundRoutingConfiguration>() ??
                throw new InvalidOperationException(
                    "IOutboundRoutingConfiguration not found, " +
                    "WithConnectionToMessageBroker has not been called.");

            outboundRoutingConfiguration.PublishOutboundMessagesToInternalBus = true;

            return silverbackBuilder;
        }
    }
}
