// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Adds the AllowDuplicateEndpointRegistrations method to the <see cref="BrokerOptionsBuilder" />.
/// </content>
public sealed partial class BrokerOptionsBuilder
{
    /// <summary>
    ///     Enables registration of duplicate endpoints.
    /// </summary>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public BrokerOptionsBuilder AllowDuplicateEndpointRegistrations()
    {
        IOutboundRoutingConfiguration outboundRoutingConfiguration =
            SilverbackBuilder.Services.GetSingletonServiceInstance<IOutboundRoutingConfiguration>() ??
            throw new InvalidOperationException("IOutboundRoutingConfiguration not found, WithConnectionToMessageBroker has not been called.");

        outboundRoutingConfiguration.IdempotentEndpointRegistration = false;

        return this;
    }
}
