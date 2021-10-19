// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Adds the <c>AddOutbound</c> method to the <see cref="EndpointsConfigurationBuilder" />.
/// </content>
public partial class EndpointsConfigurationBuilder
{
    /// <summary>
    ///     Configures an outbound endpoint with the specified producer configuration.
    /// </summary>
    /// <param name="configuration">
    ///     The producer configuration.
    /// </param>
    /// <param name="preloadProducers">
    ///     Specifies whether the producers must be immediately instantiated and connected. When <c>false</c> the
    ///     <see cref="IProducer" /> will be created only when the first message is about to be produced.
    /// </param>
    /// <typeparam name="TMessage">
    ///     The type of the messages to be published to this endpoint.
    /// </typeparam>
    /// <returns>
    ///     The <see cref="EndpointsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public EndpointsConfigurationBuilder AddOutbound<TMessage>(ProducerConfiguration configuration, bool preloadProducers = true) =>
        AddOutbound(typeof(TMessage), configuration, preloadProducers);

    /// <summary>
    ///     Configures an outbound endpoint with the specified producer configuration.
    /// </summary>
    /// <param name="messageType">
    ///     The type of the messages to be published to this endpoint.
    /// </param>
    /// <param name="configuration">
    ///     The producer configuration.
    /// </param>
    /// <param name="preloadProducers">
    ///     Specifies whether the producers must be immediately instantiated and connected. When <c>false</c> the
    ///     <see cref="IProducer" /> will be created only when the first message is about to be produced.
    /// </param>
    /// <returns>
    ///     The <see cref="EndpointsConfigurationBuilder" /> so that additional calls can be chained.
    /// </returns>
    public EndpointsConfigurationBuilder AddOutbound(Type messageType, ProducerConfiguration configuration, bool preloadProducers = true)
    {
        Check.NotNull(messageType, nameof(messageType));
        Check.NotNull(configuration, nameof(configuration));

        IOutboundRoutingConfiguration routing = GetOutboundRoutingConfiguration();

        if (routing.IdempotentEndpointRegistration && routing.IsAlreadyRegistered(messageType, configuration))
            return this;

        routing.AddRoute(messageType, configuration);

        if (preloadProducers)
            ServiceProvider.GetRequiredService<ProducersPreloader>().PreloadProducer(configuration);

        return this;
    }

    private IOutboundRoutingConfiguration GetOutboundRoutingConfiguration() =>
        ServiceProvider.GetRequiredService<IOutboundRoutingConfiguration>();
}
