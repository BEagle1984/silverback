// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>WithConnectionToMessageBroker</c> method to the <see cref="ISilverbackBuilder" />.
    /// </summary>
    public static class SilverbackBuilderWithConnectionToExtensions
    {
        /// <summary>
        ///     Registers the types needed to connect with a message broker.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the services to.
        /// </param>
        /// <param name="optionsAction">
        ///     Additional options such as the actual message brokers to be used.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder WithConnectionToMessageBroker(
            this ISilverbackBuilder silverbackBuilder,
            Action<IBrokerOptionsBuilder>? optionsAction = null)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            // Outbound Routing
            silverbackBuilder
                .AddScopedBehavior<OutboundRouterBehavior>()
                .AddScopedBehavior<ProduceBehavior>()
                .Services
                .AddSingleton<IOutboundRoutingConfiguration>(new OutboundRoutingConfiguration());

            // Broker Collection
            silverbackBuilder.Services
                .AddSingleton<IBrokerCollection, BrokerCollection>();

            // Logging
            silverbackBuilder.Services
                .AddSingleton(typeof(IInboundLogger<>), typeof(InboundLogger<>))
                .AddSingleton(typeof(IOutboundLogger<>), typeof(OutboundLogger<>))
                .AddSingleton(new LoggerCollection());

            // Activities
            silverbackBuilder.Services.AddSingleton<IActivityEnricherFactory, ActivityEnricherFactory>();

            // Transactional Lists
            silverbackBuilder.Services
                .AddSingleton(typeof(TransactionalListSharedItems<>))
                .AddSingleton(typeof(TransactionalDictionarySharedItems<,>));

            // Event Handlers
            silverbackBuilder.Services.AddSingleton<IBrokerCallbacksInvoker, BrokerCallbackInvoker>();

            var optionsBuilder = new BrokerOptionsBuilder(silverbackBuilder);
            optionsAction?.Invoke(optionsBuilder);
            optionsBuilder.CompleteWithDefaults();

            return silverbackBuilder;
        }
    }
}
