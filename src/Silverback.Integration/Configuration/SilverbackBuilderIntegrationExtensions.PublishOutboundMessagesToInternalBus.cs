// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Producing.Routing;
using Silverback.Util;

namespace Silverback.Configuration;

/// <content>
///     Adds the PublishOutboundMessagesToInternalBus method to the <see cref="SilverbackBuilder" />.
/// </content>
public static partial class SilverbackBuilderIntegrationExtensions
{
    /// <summary>
    ///     Enables the legacy behavior where the messages to be routed through an outbound connector are also
    ///     being published to the internal bus, to be locally subscribed. This is now disabled by default.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder PublishOutboundMessagesToInternalBus(this SilverbackBuilder builder)
    {
        Check.NotNull(builder, nameof(builder));

        IOutboundRoutingConfiguration outboundRoutingConfiguration =
            builder.Services.GetSingletonServiceInstance<IOutboundRoutingConfiguration>() ??
            throw new InvalidOperationException("IOutboundRoutingConfiguration not found, WithConnectionToMessageBroker has not been called.");

        outboundRoutingConfiguration.PublishOutboundMessagesToInternalBus = true;

        return builder;
    }
}
