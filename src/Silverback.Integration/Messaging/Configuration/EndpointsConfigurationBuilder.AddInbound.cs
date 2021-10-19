// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Adds the <c>AddInbound</c> method to the <see cref="EndpointsConfigurationBuilder" />.
/// </content>
public partial class EndpointsConfigurationBuilder
{
    /// <summary>
    ///     Configures an inbound endpoint with the specified consumer configuration.
    /// </summary>
    /// <param name="configuration">
    ///     The consumer configuration.
    /// </param>
    /// <param name="consumersCount">
    ///     The number of consumers to be instantiated. The default is 1.
    /// </param>
    /// <returns>
    ///     The <see cref="EndpointsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    /// <remarks>
    ///     Multiple calls to this methods will cause multiple consumers to be instantiated, which could mean multiple connections being
    ///     issues and more resources being used (depending on the actual message broker implementation). The consumer endpoint might allow
    ///     to define multiple endpoints at once, to efficiently instantiate a single consumer for all of them.
    /// </remarks>
    public EndpointsConfigurationBuilder AddInbound(ConsumerConfiguration configuration, int consumersCount = 1)
    {
        Check.NotNull(configuration, nameof(configuration));

        if (consumersCount <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(consumersCount),
                consumersCount,
                "The consumers count must be greater or equal to 1.");
        }

        IBrokerCollection? brokerCollection = ServiceProvider.GetRequiredService<IBrokerCollection>();
        ISilverbackLogger<EndpointsConfigurationBuilder>? logger =
            ServiceProvider.GetRequiredService<ISilverbackLogger<EndpointsConfigurationBuilder>>();

        if (!configuration.IsValid(logger))
            return this;

        for (int i = 0; i < consumersCount; i++)
        {
            brokerCollection.AddConsumer(configuration);
        }

        return this;
    }
}
