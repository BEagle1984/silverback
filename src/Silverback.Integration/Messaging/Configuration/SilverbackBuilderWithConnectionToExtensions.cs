// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    /// Adds the <c>WithConnectionToMessageBroker</c> method to the <see cref="ISilverbackBuilder"/>.
    /// </summary>
    public static class SilverbackBuilderWithConnectionToExtensions
    {
        /// <summary>
        ///     Registers the types needed to connect with a message broker.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <param name="optionsAction">Additional options (such as message broker type and connectors).</param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder WithConnectionToMessageBroker(
            this ISilverbackBuilder silverbackBuilder,
            Action<IBrokerOptionsBuilder>? optionsAction = null)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services
                .AddSingleton<IBrokerCollection, BrokerCollection>()
                .AddSingleton<IErrorPolicyBuilder, ErrorPolicyBuilder>()
                .AddSingletonBrokerBehavior<ActivityProducerBehavior>()
                .AddSingletonBrokerBehavior<ActivityConsumerBehavior>();

            var options = new BrokerOptionsBuilder(silverbackBuilder);
            optionsAction?.Invoke(options);
            options.CompleteWithDefaults();

            return silverbackBuilder;
        }

        /// <summary>
        ///     Registers the message broker of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the message broker implementation.</typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <param name="optionsAction">Additional options (such as connectors).</param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        [Obsolete("Use WithConnectionToMessageBroker instead")]
        public static ISilverbackBuilder WithConnectionTo<T>(
            this ISilverbackBuilder silverbackBuilder,
            Action<IBrokerOptionsBuilder>? optionsAction = null)
            where T : class, IBroker
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            return silverbackBuilder.WithConnectionToMessageBroker(
                options =>
                {
                    options.AddBroker<T>();
                    optionsAction?.Invoke(options);
                });
        }
    }
}
