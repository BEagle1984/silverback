// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Configuration
{
    /// <content>
    ///     Adds the AddBehavior methods to the <see cref="SilverbackBuilder" />.
    /// </content>
    public partial class SilverbackBuilder
    {
        /// <summary>
        ///     Adds a scoped behavior of the type specified in <paramref name="behaviorType" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="behaviorType">
        ///     The type of the behavior to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public SilverbackBuilder AddTransientBehavior(Type behaviorType)
        {
            Check.NotNull(behaviorType, nameof(behaviorType));

            Services.AddTransient(typeof(IBehavior), behaviorType);

            return this;
        }

        /// <summary>
        ///     Adds a scoped behavior of the type specified in <typeparamref name="TBehavior" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBehavior">
        ///     The type of the behavior to add.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public SilverbackBuilder AddTransientBehavior<TBehavior>()
            where TBehavior : class, IBehavior =>
            AddTransientBehavior(typeof(TBehavior));

        /// <summary>
        ///     Adds a scoped behavior with a factory specified in <paramref name="implementationFactory" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public SilverbackBuilder AddTransientBehavior(Func<IServiceProvider, IBehavior> implementationFactory)
        {
            Check.NotNull(implementationFactory, nameof(implementationFactory));

            Services.AddTransient(typeof(IBehavior), implementationFactory);

            return this;
        }

        /// <summary>
        ///     Adds a scoped behavior of the type specified in <paramref name="behaviorType" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="behaviorType">
        ///     The type of the behavior to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public SilverbackBuilder AddScopedBehavior(Type behaviorType)
        {
            Check.NotNull(behaviorType, nameof(behaviorType));

            Services.AddScoped(typeof(IBehavior), behaviorType);

            return this;
        }

        /// <summary>
        ///     Adds a scoped behavior of the type specified in <typeparamref name="TBehavior" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBehavior">
        ///     The type of the behavior to add.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public SilverbackBuilder AddScopedBehavior<TBehavior>()
            where TBehavior : class, IBehavior =>
            AddScopedBehavior(typeof(TBehavior));

        /// <summary>
        ///     Adds a scoped behavior with a factory specified in <paramref name="implementationFactory" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public SilverbackBuilder AddScopedBehavior(Func<IServiceProvider, IBehavior> implementationFactory)
        {
            Check.NotNull(implementationFactory, nameof(implementationFactory));

            Services.AddScoped(typeof(IBehavior), implementationFactory);

            return this;
        }

        /// <summary>
        ///     Adds a singleton behavior of the type specified in <paramref name="behaviorType" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="behaviorType">
        ///     The type of the behavior to register and the implementation to use.
        /// </param>
        /// <returns>
        ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public SilverbackBuilder AddSingletonBehavior(Type behaviorType)
        {
            Check.NotNull(behaviorType, nameof(behaviorType));

            Services.AddSingleton(typeof(IBehavior), behaviorType);

            return this;
        }

        /// <summary>
        ///     Adds a singleton behavior of the type specified in <typeparamref name="TBehavior" /> to the
        ///     <see cref="IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBehavior">
        ///     The type of the behavior to add.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public SilverbackBuilder AddSingletonBehavior<TBehavior>()
            where TBehavior : class, IBehavior =>
            AddSingletonBehavior(typeof(TBehavior));

        /// <summary>
        ///     Adds a singleton behavior with a factory specified in <paramref name="implementationFactory" /> to
        ///     the <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="implementationFactory">
        ///     The factory that creates the service.
        /// </param>
        /// <returns>
        ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public SilverbackBuilder AddSingletonBehavior(Func<IServiceProvider, IBehavior> implementationFactory)
        {
            Check.NotNull(implementationFactory, nameof(implementationFactory));

            Services.AddSingleton(typeof(IBehavior), implementationFactory);

            return this;
        }

        /// <summary>
        ///     Adds a singleton behavior with an instance specified in <paramref name="implementationInstance" />
        ///     to the <see cref="IServiceCollection" />.
        /// </summary>
        /// <param name="implementationInstance">
        ///     The instance of the service.
        /// </param>
        /// <returns>
        ///     The <see cref="SilverbackBuilder" /> so that additional calls can be chained.
        /// </returns>
        public SilverbackBuilder AddSingletonBehavior(IBehavior implementationInstance)
        {
            Check.NotNull(implementationInstance, nameof(implementationInstance));

            Services.AddSingleton(typeof(IBehavior), implementationInstance);

            return this;
        }
    }
}
