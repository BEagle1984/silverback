// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Linq;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class SilverbackBuilderExtensions
    {
        #region WithConnectionTo

        /// <summary>
        ///     Registers the message broker of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the message broker implementation.</typeparam>
        /// <param name="builder"></param>
        /// <param name="optionsAction">Additional options (such as connectors).</param>
        /// <returns></returns>
        public static ISilverbackBuilder WithConnectionTo<T>(
            this ISilverbackBuilder builder,
            Action<BrokerOptionsBuilder> optionsAction = null)
            where T : class, IBroker
        {
            builder.Services
                .AddSingleton<IBroker, T>()
                .AddSingleton<ErrorPolicyBuilder>()
                .AddSingleton<MessageIdProvider>()
                .AddSingleton<MessageLogger>()
                .AddSingletonBrokerBehavior<ActivityProducerBehavior>()
                .AddSingletonBrokerBehavior<ActivityConsumerBehavior>();

            var options = new BrokerOptionsBuilder(builder);
            FindOptionsConfigurator<T>()?.Configure(builder, options);
            optionsAction?.Invoke(options);
            options.CompleteWithDefaults();

            return builder;
        }

        private static IBrokerOptionsConfigurator<TBroker> FindOptionsConfigurator<TBroker>()
            where TBroker : IBroker
        {
            var type = typeof(TBroker).Assembly.GetTypes()
                .FirstOrDefault(t => typeof(IBrokerOptionsConfigurator<TBroker>).IsAssignableFrom(t));

            if (type == null)
                return null;

            return (IBrokerOptionsConfigurator<TBroker>) Activator.CreateInstance(type);
        }

        #endregion

        #region RegisterConfigurator

        /// <summary>
        ///     Adds an <see cref="IEndpointsConfigurator" /> to be used to setup the broker endpoints.
        /// </summary>
        /// <param name="builder"></param>
        /// <typeparam name="TConfigurator">The type of the <see cref="IEndpointsConfigurator" /> to add.</typeparam>
        /// <returns></returns>
        public static ISilverbackBuilder AddEndpointsConfigurator<TConfigurator>(this ISilverbackBuilder builder)
            where TConfigurator : class, IEndpointsConfigurator
        {
            builder.Services.AddEndpointsConfigurator<TConfigurator>();
            return builder;
        }

        /// <summary>
        ///     Adds an <see cref="IEndpointsConfigurator" /> to be used to setup the broker endpoints.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="configuratorType">The type of the <see cref="IEndpointsConfigurator" /> to add.</param>
        /// <returns></returns>
        public static ISilverbackBuilder AddEndpointsConfigurator(
            this ISilverbackBuilder builder,
            Type configuratorType)
        {
            builder.Services.AddEndpointsConfigurator(configuratorType);
            return builder;
        }

        /// <summary>
        ///     Adds an <see cref="IEndpointsConfigurator" /> to be used to setup the broker endpoints.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="implementationFactory">The factory that creates the <see cref="IEndpointsConfigurator" /> to add.</param>
        /// <returns></returns>
        public static ISilverbackBuilder AddEndpointsConfigurator(
            this ISilverbackBuilder builder,
            Func<IServiceProvider, IEndpointsConfigurator> implementationFactory)
        {
            builder.Services.AddEndpointsConfigurator(implementationFactory);
            return builder;
        }

        #endregion

        #region BrokerBehaviors (AddSingletonBrokerBehavior)

        /// <summary>
        ///     Adds a singleton behavior of the type specified in <paramref name="behaviorType" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="behaviorType">The type of the behavior to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static ISilverbackBuilder AddSingletonBrokerBehavior(this ISilverbackBuilder builder, Type behaviorType)
        {
            builder.Services.AddSingletonBrokerBehavior(behaviorType);
            return builder;
        }

        /// <summary>
        ///     Adds a singleton behavior of the type specified in <typeparamref name="TBehavior" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBehavior">The type of the behavior to add.</typeparam>
        /// <param name="builder"></param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static ISilverbackBuilder AddSingletonBrokerBehavior<TBehavior>(this ISilverbackBuilder builder)
            where TBehavior : class, IBrokerBehavior
        {
            builder.Services.AddSingletonBrokerBehavior<TBehavior>();
            return builder;
        }

        /// <summary>
        ///     Adds a singleton behavior with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static ISilverbackBuilder AddSingletonBrokerBehavior(
            this ISilverbackBuilder builder,
            Func<IServiceProvider, IBrokerBehavior> implementationFactory)
        {
            builder.Services.AddSingletonBrokerBehavior(implementationFactory);
            return builder;
        }

        /// <summary>
        ///     Adds a singleton behavior with an
        ///     instance specified in <paramref name="implementationInstance" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="implementationInstance">The instance of the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        /// <seealso cref="F:Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton" />
        public static ISilverbackBuilder AddSingletonBrokerBehavior(
            this ISilverbackBuilder builder,
            IBrokerBehavior implementationInstance)
        {
            builder.Services.AddSingletonBrokerBehavior(implementationInstance);
            return builder;
        }

        #endregion
    }
}