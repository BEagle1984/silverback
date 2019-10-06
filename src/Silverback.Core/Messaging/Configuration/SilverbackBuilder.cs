// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Configuration
{
    internal class SilverbackBuilder : ISilverbackBuilder
    {
        public SilverbackBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }

        #region Subscribers

        #region AddTransientSubscriber

        /// <summary>
        /// Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="baseType">The subscribers base class or interface.</param>
        /// <param name="subscriberType">The type of the subscriber to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddTransientSubscriber(Type baseType, Type subscriberType)
        {
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));
            if (subscriberType == null) throw new ArgumentNullException(nameof(subscriberType));

            Services
                .AddTransient(subscriberType)
                .AddTransient(baseType, subscriberType);

            return this;
        }

        /// <summary>
        /// Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="subscriberType">The type of the subscriber to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddTransientSubscriber(Type subscriberType) =>
            AddTransientSubscriber(typeof(ISubscriber), subscriberType);

        /// <summary>
        /// Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">The subscribers base class or interface.</typeparam>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddTransientSubscriber<TBase, TSubscriber>()
            where TSubscriber : class, ISubscriber =>
            AddTransientSubscriber(typeof(TBase), typeof(TSubscriber));

        /// <summary>
        /// Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddTransientSubscriber<TSubscriber>()
            where TSubscriber : class, ISubscriber =>
            AddTransientSubscriber<ISubscriber, TSubscriber>();

        /// <summary>
        /// Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> with a
        /// factory specified in <paramref name="implementationFactory" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="baseType">The subscribers base class or interface.</param>
        /// <param name="subscriberType">The type of the subscriber to register.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddTransientSubscriber(
            Type baseType,
            Type subscriberType,
            Func<IServiceProvider, object> implementationFactory)
        {
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));
            if (subscriberType == null) throw new ArgumentNullException(nameof(subscriberType));
            if (implementationFactory == null) throw new ArgumentNullException(nameof(implementationFactory));

            Services
                .AddTransient(subscriberType, implementationFactory)
                .AddTransient(baseType, implementationFactory);

            return this;
        }

        /// <summary>
        /// Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> with a
        /// factory specified in <paramref name="implementationFactory" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="subscriberType">The type of the subscriber to register.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddTransientSubscriber(
            Type subscriberType,
            Func<IServiceProvider, ISubscriber> implementationFactory) =>
            AddTransientSubscriber(typeof(ISubscriber), subscriberType, implementationFactory);

        /// <summary>
        /// Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> with a
        /// factory specified in <paramref name="implementationFactory" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">The subscribers base class or interface.</typeparam>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddTransientSubscriber<TBase, TSubscriber>(
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber =>
            AddTransientSubscriber(typeof(TBase), typeof(TSubscriber), implementationFactory);

        /// <summary>
        /// Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> with a
        /// factory specified in <paramref name="implementationFactory" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddTransientSubscriber<TSubscriber>(
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber =>
            AddTransientSubscriber<ISubscriber, TSubscriber>(implementationFactory);

        #endregion

        #region AddScopedSubscriber

        /// <summary>
        /// Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="baseType">The subscribers base class or interface.</param>
        /// <param name="subscriberType">The type of the subscriber to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddScopedSubscriber(Type baseType, Type subscriberType)
        {
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));
            if (subscriberType == null) throw new ArgumentNullException(nameof(subscriberType));

            Services
                .AddScoped(subscriberType)
                .AddScoped(baseType, subscriberType);

            return this;
        }

        /// <summary>
        /// Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="subscriberType">The type of the subscriber to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddScopedSubscriber(Type subscriberType) =>
            AddScopedSubscriber(typeof(ISubscriber), subscriberType);

        /// <summary>
        /// Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">The subscribers base class or interface.</typeparam>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddScopedSubscriber<TBase, TSubscriber>()
            where TSubscriber : class, ISubscriber =>
            AddScopedSubscriber(typeof(TBase), typeof(TSubscriber));

        /// <summary>
        /// Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddScopedSubscriber<TSubscriber>()
            where TSubscriber : class, ISubscriber =>
            AddScopedSubscriber<ISubscriber, TSubscriber>();

        /// <summary>
        /// Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> with a
        /// factory specified in <paramref name="implementationFactory" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="baseType">The subscribers base class or interface.</param>
        /// <param name="subscriberType">The type of the subscriber to register.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddScopedSubscriber(
            Type baseType,
            Type subscriberType,
            Func<IServiceProvider, object> implementationFactory)
        {
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));
            if (subscriberType == null) throw new ArgumentNullException(nameof(subscriberType));
            if (implementationFactory == null) throw new ArgumentNullException(nameof(implementationFactory));

            Services
                .AddScoped(subscriberType, implementationFactory)
                .AddScoped(baseType, implementationFactory);

            return this;
        }

        /// <summary>
        /// Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> with a
        /// factory specified in <paramref name="implementationFactory" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="subscriberType">The type of the subscriber to register.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddScopedSubscriber(
            Type subscriberType,
            Func<IServiceProvider, ISubscriber> implementationFactory) =>
            AddScopedSubscriber(typeof(ISubscriber), subscriberType, implementationFactory);

        /// <summary>
        /// Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> with a
        /// factory specified in <paramref name="implementationFactory" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">The subscribers base class or interface.</typeparam>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddScopedSubscriber<TBase, TSubscriber>(
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber =>
            AddScopedSubscriber(typeof(TBase), typeof(TSubscriber), implementationFactory);

        /// <summary>
        /// Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> with a
        /// factory specified in <paramref name="implementationFactory" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddScopedSubscriber<TSubscriber>(
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber =>
            AddScopedSubscriber<ISubscriber, TSubscriber>(implementationFactory);

        #endregion

        #region AddSingletonSubscriber

        /// <summary>
        /// Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="baseType">The subscribers base class or interface.</param>
        /// <param name="subscriberType">The type of the subscriber to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonSubscriber(Type baseType, Type subscriberType)
        {
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));
            if (subscriberType == null) throw new ArgumentNullException(nameof(subscriberType));

            Services
                .AddSingleton(subscriberType)
                .AddSingleton(baseType, subscriberType);

            return this;
        }

        /// <summary>
        /// Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="subscriberType">The type of the subscriber to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonSubscriber(Type subscriberType) =>
            AddSingletonSubscriber(typeof(ISubscriber), subscriberType);

        /// <summary>
        /// Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">The subscribers base class or interface.</typeparam>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonSubscriber<TBase, TSubscriber>()
            where TSubscriber : class, ISubscriber =>
            AddSingletonSubscriber(typeof(TBase), typeof(TSubscriber));

        /// <summary>
        /// Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonSubscriber<TSubscriber>()
            where TSubscriber : class, ISubscriber =>
            AddSingletonSubscriber<ISubscriber, TSubscriber>();

        /// <summary>
        /// Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> with a
        /// factory specified in <paramref name="implementationFactory" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="baseType">The subscribers base class or interface.</param>
        /// <param name="subscriberType">The type of the subscriber to register.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonSubscriber(
            Type baseType,
            Type subscriberType,
            Func<IServiceProvider, object> implementationFactory)
        {
            if (baseType == null) throw new ArgumentNullException(nameof(baseType));
            if (subscriberType == null) throw new ArgumentNullException(nameof(subscriberType));
            if (implementationFactory == null) throw new ArgumentNullException(nameof(implementationFactory));

            Services
                .AddSingleton(subscriberType, implementationFactory)
                .AddSingleton(baseType, implementationFactory);

            return this;
        }

        /// <summary>
        /// Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> with a
        /// factory specified in <paramref name="implementationFactory" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="subscriberType">The type of the subscriber to register.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonSubscriber(
            Type subscriberType,
            Func<IServiceProvider, ISubscriber> implementationFactory) =>
            AddSingletonSubscriber(typeof(ISubscriber), subscriberType, implementationFactory);

        /// <summary>
        /// Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> with a
        /// factory specified in <paramref name="implementationFactory" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">The subscribers base class or interface.</typeparam>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonSubscriber<TBase, TSubscriber>(
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber =>
            AddSingletonSubscriber(typeof(TBase), typeof(TSubscriber), implementationFactory);

        /// <summary>
        /// Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> with a
        /// factory specified in <paramref name="implementationFactory" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonSubscriber<TSubscriber>(
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber =>
            AddSingletonSubscriber<ISubscriber, TSubscriber>(implementationFactory);

        /// <summary>
        /// Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> with an
        /// instance specified in <paramref name="implementationInstance" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="baseType">The subscribers base class or interface.</param>
        /// <param name="subscriberType">The type of the subscriber to register.</param>
        /// <param name="implementationInstance">The instance of the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonSubscriber(
            Type baseType,
            Type subscriberType,
            ISubscriber implementationInstance)
        {
            if (implementationInstance == null) throw new ArgumentNullException(nameof(implementationInstance));

            Services
                .AddSingleton(subscriberType, implementationInstance)
                .AddSingleton(baseType, implementationInstance);

            return this;
        }

        /// <summary>
        /// Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> with an
        /// instance specified in <paramref name="implementationInstance" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="subscriberType">The type of the subscriber to register.</param>
        /// <param name="implementationInstance">The instance of the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonSubscriber(Type subscriberType, ISubscriber implementationInstance) =>
            AddSingletonSubscriber(typeof(ISubscriber), subscriberType, implementationInstance);

        /// <summary>
        /// Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> with an
        /// instance specified in <paramref name="implementationInstance" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">The subscribers base class or interface.</typeparam>
        /// <typeparam name="TSubscriber">The type of the subscriber to register.</typeparam>
        /// <param name="implementationInstance">The instance of the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonSubscriber<TBase, TSubscriber>(TSubscriber implementationInstance)
            where TSubscriber : class, ISubscriber =>
            AddSingletonSubscriber(typeof(TBase), typeof(TSubscriber), implementationInstance);

        /// <summary>
        /// Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> with an
        /// instance specified in <paramref name="implementationInstance" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="implementationInstance">The instance of the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonSubscriber<TSubscriber>(TSubscriber implementationInstance)
            where TSubscriber : class, ISubscriber =>
            AddSingletonSubscriber(typeof(ISubscriber), typeof(TSubscriber), implementationInstance);

        #endregion

        #endregion

        #region Behaviors

        #region AddTransientBehavior

        /// <summary>
        /// Adds a scoped behavior of the type specified in <paramref name="behaviorType" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="behaviorType">The type of the behavior to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddTransientBehavior(Type behaviorType)
        {
            if (behaviorType == null) throw new ArgumentNullException(nameof(behaviorType));

            Services.AddTransient(typeof(IBehavior), behaviorType);

            return this;
        }

        /// <summary>
        /// Adds a scoped behavior of the type specified in <typeparamref name="TBehavior" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBehavior">The type of the behavior to add.</typeparam>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddTransientBehavior<TBehavior>()
            where TBehavior : class, IBehavior =>
            AddTransientBehavior(typeof(TBehavior));

        /// <summary>
        /// Adds a scoped behavior of the type specified in <paramref name="behaviorType" /> with a
        /// factory specified in <paramref name="implementationFactory" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddTransientBehavior(Func<IServiceProvider, IBehavior> implementationFactory)
        {
            if (implementationFactory == null) throw new ArgumentNullException(nameof(implementationFactory));

            Services.AddTransient(typeof(IBehavior), implementationFactory);

            return this;
        }

        #endregion

        #region AddScopedBehavior

        /// <summary>
        /// Adds a scoped behavior of the type specified in <paramref name="behaviorType" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="behaviorType">The type of the behavior to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddScopedBehavior(Type behaviorType)
        {
            if (behaviorType == null) throw new ArgumentNullException(nameof(behaviorType));

            Services.AddScoped(typeof(IBehavior), behaviorType);

            return this;
        }

        /// <summary>
        /// Adds a scoped behavior of the type specified in <typeparamref name="TBehavior" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBehavior">The type of the behavior to add.</typeparam>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddScopedBehavior<TBehavior>()
            where TBehavior : class, IBehavior =>
            AddScopedBehavior(typeof(TBehavior));

        /// <summary>
        /// Adds a scoped behavior of the type specified in <paramref name="behaviorType" /> with a
        /// factory specified in <paramref name="implementationFactory" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddScopedBehavior(Func<IServiceProvider, IBehavior> implementationFactory)
        {
            if (implementationFactory == null) throw new ArgumentNullException(nameof(implementationFactory));

            Services.AddScoped(typeof(IBehavior), implementationFactory);

            return this;
        }

        #endregion

        #region AddSingletonBehavior

        /// <summary>
        /// Adds a singleton behavior of the type specified in <paramref name="behaviorType" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="behaviorType">The type of the behavior to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonBehavior(Type behaviorType)
        {
            if (behaviorType == null) throw new ArgumentNullException(nameof(behaviorType));

            Services.AddSingleton(typeof(IBehavior), behaviorType);

            return this;
        }

        /// <summary>
        /// Adds a singleton behavior of the type specified in <typeparamref name="TBehavior" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBehavior">The type of the behavior to add.</typeparam>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonBehavior<TBehavior>()
            where TBehavior : class, IBehavior =>
            AddSingletonBehavior(typeof(TBehavior));

        /// <summary>
        /// Adds a singleton behavior with a
        /// factory specified in <paramref name="implementationFactory" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonBehavior(Func<IServiceProvider, IBehavior> implementationFactory)
        {
            if (implementationFactory == null) throw new ArgumentNullException(nameof(implementationFactory));

            Services.AddSingleton(typeof(IBehavior), implementationFactory);

            return this;
        }

        /// <summary>
        /// Adds a singleton behavior with an
        /// instance specified in <paramref name="implementationInstance" /> to the
        /// specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="implementationInstance">The instance of the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        /// <seealso cref="F:Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton" />
        public ISilverbackBuilder AddSingletonBehavior(IBehavior implementationInstance)
        {
            if (implementationInstance == null) throw new ArgumentNullException(nameof(implementationInstance));

            Services.AddSingleton(typeof(IBehavior), implementationInstance);

            return this;
        }

        #endregion

        #endregion
    }
}