// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c> AddSingletonBrokerBehavior </c> method to the <see cref="ISilverbackBuilder" />.
    /// </summary>
    public static class SilverbackBuilderAddBrokerBehaviorExtensions
    {
        /// <summary>
        ///     Adds a singleton behavior of the type specified in <paramref name="behaviorType" /> to the
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the services to.
        /// </param>
        /// <param name="behaviorType">
        ///     The type of the behavior to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonBrokerBehavior(
            this ISilverbackBuilder silverbackBuilder,
            Type behaviorType)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddSingletonBrokerBehavior(behaviorType);

            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton behavior of the type specified in <typeparamref name="TBehavior" /> to the
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBehavior"> The type of the behavior to add. </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonBrokerBehavior<TBehavior>(
            this ISilverbackBuilder silverbackBuilder)
            where TBehavior : class, IBrokerBehavior
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddSingletonBrokerBehavior<TBehavior>();

            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton behavior with a factory specified in <paramref name="implementationFactory" /> to
        ///     the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the services to.
        /// </param>
        /// <param name="implementationFactory"> The factory that creates the service. </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonBrokerBehavior(
            this ISilverbackBuilder silverbackBuilder,
            Func<IServiceProvider, IBrokerBehavior> implementationFactory)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddSingletonBrokerBehavior(implementationFactory);

            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton behavior with an instance specified in <paramref name="implementationInstance" />
        ///     to the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the services to.
        /// </param>
        /// <param name="implementationInstance"> The instance of the service. </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonBrokerBehavior(
            this ISilverbackBuilder silverbackBuilder,
            IBrokerBehavior implementationInstance)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));

            silverbackBuilder.Services.AddSingletonBrokerBehavior(implementationInstance);

            return silverbackBuilder;
        }
    }
}
