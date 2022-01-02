// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Outbound.Enrichers;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Util;

namespace Silverback.Configuration;

/// <content>
///     Adds the WithConnectionToMessageBroker method to the <see cref="SilverbackBuilder" />.
/// </content>
public static partial class SilverbackBuilderIntegrationExtensions
{
    /// <summary>
    ///     Registers the types needed to connect with a message broker.
    /// </summary>
    /// <param name="builder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    /// <param name="optionsAction">
    ///     Additional options such as the actual message brokers to be used.
    /// </param>
    /// <returns>
    ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
    /// </returns>
    public static SilverbackBuilder WithConnectionToMessageBroker(
        this SilverbackBuilder builder,
        Action<BrokerOptionsBuilder>? optionsAction = null)
    {
        Check.NotNull(builder, nameof(builder));

        // Outbound Routing
        builder
            .AddScopedBehavior<OutboundRouterBehavior>()
            .AddScopedBehavior<ProduceBehavior>()
            .Services
            .AddSingleton<IOutboundEnvelopeFactory, OutboundEnvelopeFactory>()
            .AddSingleton<IOutboundRoutingConfiguration>(new OutboundRoutingConfiguration())
            .AddSingleton<ProducersPreloader>();

        // Broker Collection
        builder.Services
            .AddSingleton<IBrokerCollection, BrokerCollection>();

        // Logging
        builder.Services
            .AddSingleton(typeof(IInboundLogger<>), typeof(InboundLogger<>))
            .AddSingleton(typeof(IOutboundLogger<>), typeof(OutboundLogger<>))
            .AddSingleton<InboundLoggerFactory>()
            .AddSingleton<OutboundLoggerFactory>()
            .AddSingleton<BrokerLogEnricherFactory>();

        // Message Enrichers
        builder.Services
            .AddSingleton<IBrokerOutboundMessageEnrichersFactory, BrokerOutboundMessageEnrichersFactory>();

        // Activities
        builder.Services.AddSingleton<IActivityEnricherFactory, ActivityEnricherFactory>();

        // Transactional Lists
        builder.Services
            .AddSingleton(typeof(TransactionalListSharedItems<>))
            .AddSingleton(typeof(TransactionalDictionarySharedItems<,>));

        // Event Handlers
        builder.Services.AddSingleton<IBrokerCallbacksInvoker, BrokerCallbackInvoker>();

        BrokerOptionsBuilder optionsBuilder = new(builder);
        optionsAction?.Invoke(optionsBuilder);
        optionsBuilder.CompleteWithDefaults();

        return builder;
    }
}
