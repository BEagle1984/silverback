// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>AddTransientBehavior</c>, <c>AddScopedBehavior</c> and <c>AddSingletonBehavior</c> methods
    ///     to the <see cref="ISilverbackBuilder" />.
    /// </summary>
    public static class SilverbackBuilderAddBehaviorExtensions
    {
        /// <summary>
        ///     Adds a scoped behavior of the type specified in <paramref name="behaviorType" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the behavior to.
        /// </param>
        /// <param name="behaviorType">
        ///     The type of the behavior to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddTransientBehavior(
            this ISilverbackBuilder silverbackBuilder,
            Type behaviorType)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(behaviorType, nameof(behaviorType));

            silverbackBuilder.Services.AddTransient(typeof(IBehavior), behaviorType);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a scoped behavior of the type specified in <typeparamref name="TBehavior" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBehavior">
        ///     The type of the behavior to add.
        /// </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the behavior to.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddTransientBehavior<TBehavior>(this ISilverbackBuilder silverbackBuilder)
            where TBehavior : class, IBehavior =>
            AddTransientBehavior(silverbackBuilder, typeof(TBehavior));

        /// <summary>
        ///     Adds a scoped behavior with a factory specified in <paramref name="implementationFactory" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the behavior to.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddTransientBehavior(
            this ISilverbackBuilder silverbackBuilder,
            Func<IServiceProvider, IBehavior> implementationFactory)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(implementationFactory, nameof(implementationFactory));

            silverbackBuilder.Services.AddTransient(typeof(IBehavior), implementationFactory);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a scoped behavior of the type specified in <paramref name="behaviorType" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the behavior to.
        /// </param>
        /// <param name="behaviorType">
        ///     The type of the behavior to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddScopedBehavior(this ISilverbackBuilder silverbackBuilder, Type behaviorType)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(behaviorType, nameof(behaviorType));

            silverbackBuilder.Services.AddScoped(typeof(IBehavior), behaviorType);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a scoped behavior of the type specified in <typeparamref name="TBehavior" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBehavior">
        ///     The type of the behavior to add.
        /// </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the behavior to.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddScopedBehavior<TBehavior>(this ISilverbackBuilder silverbackBuilder)
            where TBehavior : class, IBehavior =>
            AddScopedBehavior(silverbackBuilder, typeof(TBehavior));

        /// <summary>
        ///     Adds a scoped behavior with a factory specified in <paramref name="implementationFactory" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the behavior to.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddScopedBehavior(
            this ISilverbackBuilder silverbackBuilder,
            Func<IServiceProvider, IBehavior> implementationFactory)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(implementationFactory, nameof(implementationFactory));

            silverbackBuilder.Services.AddScoped(typeof(IBehavior), implementationFactory);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton behavior of the type specified in <paramref name="behaviorType" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the behavior to.
        /// </param>
        /// <param name="behaviorType">
        ///     The type of the behavior to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonBehavior(
            this ISilverbackBuilder silverbackBuilder,
            Type behaviorType)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(behaviorType, nameof(behaviorType));

            silverbackBuilder.Services.AddSingleton(typeof(IBehavior), behaviorType);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton behavior of the type specified in <typeparamref name="TBehavior" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBehavior">
        ///     The type of the behavior to add.
        /// </typeparam>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the behavior to.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonBehavior<TBehavior>(this ISilverbackBuilder silverbackBuilder)
            where TBehavior : class, IBehavior =>
            AddSingletonBehavior(silverbackBuilder, typeof(TBehavior));

        /// <summary>
        ///     Adds a singleton behavior with a factory specified in <paramref name="implementationFactory" /> to
        ///     the <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the behavior to.
        /// </param>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonBehavior(
            this ISilverbackBuilder silverbackBuilder,
            Func<IServiceProvider, IBehavior> implementationFactory)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(implementationFactory, nameof(implementationFactory));

            silverbackBuilder.Services.AddSingleton(typeof(IBehavior), implementationFactory);
            return silverbackBuilder;
        }

        /// <summary>
        ///     Adds a singleton behavior with an instance specified in <paramref name="implementationInstance" />
        ///     to the <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="silverbackBuilder">
        ///     The <see cref="ISilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add
        ///     the behavior to.
        /// </param>
        /// <param name="implementationInstance">
        ///     The instance of the service.
        /// </param>
        /// <returns>
        ///     The <see cref="ISilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static ISilverbackBuilder AddSingletonBehavior(
            this ISilverbackBuilder silverbackBuilder,
            IBehavior implementationInstance)
        {
            Check.NotNull(silverbackBuilder, nameof(silverbackBuilder));
            Check.NotNull(implementationInstance, nameof(implementationInstance));

            silverbackBuilder.Services.AddSingleton(typeof(IBehavior), implementationInstance);
            return silverbackBuilder;
        }
    }
}
