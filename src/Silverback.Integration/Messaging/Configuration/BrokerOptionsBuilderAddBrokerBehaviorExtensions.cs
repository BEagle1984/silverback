// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker.Behaviors;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c> AddSingletonBrokerBehavior </c> method to the <see cref="IBrokerOptionsBuilder" />.
    /// </summary>
    public static class BrokerOptionsBuilderAddBrokerBehaviorExtensions
    {
        /// <summary>
        ///     Adds a singleton behavior of the type specified in <paramref name="behaviorType" /> to the
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <param name="behaviorType">
        ///     The type of the behavior to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddSingletonBrokerBehavior(
            this IBrokerOptionsBuilder brokerOptionsBuilder,
            Type behaviorType)
        {
            if (brokerOptionsBuilder == null)
                throw new ArgumentNullException(nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.SilverbackBuilder.Services.AddSingletonBrokerBehavior(behaviorType);

            return brokerOptionsBuilder;
        }

        /// <summary>
        ///     Adds a singleton behavior of the type specified in <typeparamref name="TBehavior" /> to the
        ///     <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBehavior"> The type of the behavior to add. </typeparam>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddSingletonBrokerBehavior<TBehavior>(
            this IBrokerOptionsBuilder brokerOptionsBuilder)
            where TBehavior : class, IBrokerBehavior
        {
            if (brokerOptionsBuilder == null)
                throw new ArgumentNullException(nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.SilverbackBuilder.Services.AddSingletonBrokerBehavior<TBehavior>();

            return brokerOptionsBuilder;
        }

        /// <summary>
        ///     Adds a singleton behavior with a factory specified in <paramref name="implementationFactory" /> to
        ///     the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <param name="implementationFactory"> The factory that creates the service. </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddSingletonBrokerBehavior(
            this IBrokerOptionsBuilder brokerOptionsBuilder,
            Func<IServiceProvider, IBrokerBehavior> implementationFactory)
        {
            if (brokerOptionsBuilder == null)
                throw new ArgumentNullException(nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.SilverbackBuilder.Services.AddSingletonBrokerBehavior(implementationFactory);

            return brokerOptionsBuilder;
        }

        /// <summary>
        ///     Adds a singleton behavior with an instance specified in <paramref name="implementationInstance" />
        ///     to the <see cref="Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <param name="implementationInstance"> The instance of the service. </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddSingletonBrokerBehavior(
            this IBrokerOptionsBuilder brokerOptionsBuilder,
            IBrokerBehavior implementationInstance)
        {
            if (brokerOptionsBuilder == null)
                throw new ArgumentNullException(nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.SilverbackBuilder.Services.AddSingletonBrokerBehavior(implementationInstance);

            return brokerOptionsBuilder;
        }
    }
}
