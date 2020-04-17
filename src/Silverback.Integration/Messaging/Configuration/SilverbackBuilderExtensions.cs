// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Diagnostics;
using Silverback.Messaging.Messages;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class SilverbackBuilderExtensions
    {
        #region WithConnectionTo

        /// <summary>
        ///     Registers the types needed to connect with a message broker.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="optionsAction">Additional options (such as message broker type and connectors).</param>
        /// <returns></returns>
        public static ISilverbackBuilder WithConnectionToMessageBroker(
            this ISilverbackBuilder builder,
            Action<IBrokerOptionsBuilder> optionsAction = null)
        {
            builder.Services
                .AddSingleton<IBrokerCollection, BrokerCollection>()
                .AddSingleton<ErrorPolicyBuilder>()
                .AddSingleton<MessageIdProvider>()
                .AddSingleton<MessageLogger>()
                .AddSingletonBrokerBehavior<ActivityProducerBehavior>()
                .AddSingletonBrokerBehavior<ActivityConsumerBehavior>();

            var options = new BrokerOptionsBuilder(builder);
            optionsAction?.Invoke(options);
            options.CompleteWithDefaults();

            return builder;
        }

        /// <summary>
        ///     Registers the message broker of the specified type.
        /// </summary>
        /// <typeparam name="T">The type of the message broker implementation.</typeparam>
        /// <param name="builder"></param>
        /// <param name="optionsAction">Additional options (such as connectors).</param>
        /// <returns></returns>
        [Obsolete("Use WithConnectionToMessageBroker instead")]
        public static ISilverbackBuilder WithConnectionTo<T>(
            this ISilverbackBuilder builder,
            Action<IBrokerOptionsBuilder> optionsAction = null)
            where T : class, IBroker =>
            builder.WithConnectionToMessageBroker(options =>
            {
                options.AddBroker<T>();
                optionsAction?.Invoke(options);
            });

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

        #region AddSingletonBrokerBehavior

        /// <summary>
        ///     Adds a singleton behavior of the type specified in <paramref name="behaviorType" /> to the
        ///     <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
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
        ///     <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
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
        ///     <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
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
        ///     <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="implementationInstance">The instance of the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static ISilverbackBuilder AddSingletonBrokerBehavior(
            this ISilverbackBuilder builder,
            IBrokerBehavior implementationInstance)
        {
            builder.Services.AddSingletonBrokerBehavior(implementationInstance);
            return builder;
        }

        #endregion

        #region AddSingletonOutboundRouter

        /// <summary>
        ///     Adds a singleton outbound router of the type specified in <paramref name="routerType" /> to the
        ///     <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="routerType">The type of the outbound router to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static ISilverbackBuilder AddSingletonOutboundRouter(this ISilverbackBuilder builder, Type routerType)
        {
            builder.Services.AddSingletonOutboundRouter(routerType);
            return builder;
        }

        /// <summary>
        ///     Adds a singleton outbound router of the type specified in <typeparamref name="TRouter" /> to the
        ///     <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TRouter">The type of the outbound router to add.</typeparam>
        /// <param name="builder"></param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static ISilverbackBuilder AddSingletonOutboundRouter<TRouter>(this ISilverbackBuilder builder)
            where TRouter : class, IOutboundRouter
        {
            builder.Services.AddSingletonOutboundRouter<TRouter>();
            return builder;
        }

        /// <summary>
        ///     Adds a singleton outbound router with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static ISilverbackBuilder AddSingletonOutboundRouter(
            this ISilverbackBuilder builder,
            Func<IServiceProvider, IOutboundRouter> implementationFactory)
        {
            builder.Services.AddSingletonOutboundRouter(implementationFactory);
            return builder;
        }

        /// <summary>
        ///     Adds a singleton outbound router with an
        ///     instance specified in <paramref name="implementationInstance" /> to the
        ///     <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="implementationInstance">The instance of the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public static ISilverbackBuilder AddSingletonOutboundRouter(
            this ISilverbackBuilder builder,
            IOutboundRouter implementationInstance)
        {
            builder.Services.AddSingletonOutboundRouter(implementationInstance);
            return builder;
        }

        #endregion
    }
}