// Copyright (c) 2020 Sergio Aquilini
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
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="baseType">The subscribers base class or interface.</param>
        /// <param name="subscriberType">The type of the subscriber to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddTransientSubscriber(Type baseType, Type subscriberType)
        {
            Services.AddTransientSubscriber(baseType, subscriberType);
            return this;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="subscriberType">The type of the subscriber to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddTransientSubscriber(Type subscriberType)
        {
            Services.AddTransientSubscriber(subscriberType);
            return this;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">The subscribers base class or interface.</typeparam>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddTransientSubscriber<TBase, TSubscriber>()
            where TSubscriber : class, ISubscriber
        {
            Services.AddTransientSubscriber<TBase, TSubscriber>();
            return this;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddTransientSubscriber<TSubscriber>()
            where TSubscriber : class, ISubscriber
        {
            Services.AddTransientSubscriber<TSubscriber>();
            return this;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
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
            Services.AddTransientSubscriber(baseType, subscriberType, implementationFactory);
            return this;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="subscriberType">The type of the subscriber to register.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddTransientSubscriber(
            Type subscriberType,
            Func<IServiceProvider, ISubscriber> implementationFactory)
        {
            Services.AddTransientSubscriber(subscriberType, implementationFactory);
            return this;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">The subscribers base class or interface.</typeparam>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddTransientSubscriber<TBase, TSubscriber>(
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber
        {
            Services.AddTransientSubscriber<TBase, TSubscriber>(implementationFactory);
            return this;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddTransientSubscriber<TSubscriber>(
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber
        {
            Services.AddTransientSubscriber(implementationFactory);
            return this;
        }

        #endregion

        #region AddScopedSubscriber

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="baseType">The subscribers base class or interface.</param>
        /// <param name="subscriberType">The type of the subscriber to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddScopedSubscriber(Type baseType, Type subscriberType)
        {
            Services.AddScopedSubscriber(baseType, subscriberType);
            return this;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="subscriberType">The type of the subscriber to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddScopedSubscriber(Type subscriberType)
        {
            Services.AddScopedSubscriber(subscriberType);
            return this;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">The subscribers base class or interface.</typeparam>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddScopedSubscriber<TBase, TSubscriber>()
            where TSubscriber : class, ISubscriber
        {
            Services.AddScopedSubscriber<TBase, TSubscriber>();
            return this;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddScopedSubscriber<TSubscriber>()
            where TSubscriber : class, ISubscriber
        {
            Services.AddScopedSubscriber<TSubscriber>();
            return this;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
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
            Services.AddScopedSubscriber(baseType, subscriberType, implementationFactory);
            return this;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <paramref name="subscriberType" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="subscriberType">The type of the subscriber to register.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddScopedSubscriber(
            Type subscriberType,
            Func<IServiceProvider, ISubscriber> implementationFactory)
        {
            Services.AddScopedSubscriber(subscriberType, implementationFactory);
            return this;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">The subscribers base class or interface.</typeparam>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddScopedSubscriber<TBase, TSubscriber>(
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber
        {
            Services.AddScopedSubscriber(implementationFactory);
            return this;
        }

        /// <summary>
        ///     Adds a scoped subscriber of the type specified in <typeparamref name="TSubscriber" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddScopedSubscriber<TSubscriber>(
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber
        {
            Services.AddScopedSubscriber(implementationFactory);
            return this;
        }

        #endregion

        #region AddSingletonSubscriber

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="baseType">The subscribers base class or interface.</param>
        /// <param name="subscriberType">The type of the subscriber to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonSubscriber(Type baseType, Type subscriberType)
        {
            Services.AddSingletonSubscriber(baseType, subscriberType);
            return this;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="subscriberType">The type of the subscriber to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonSubscriber(Type subscriberType)
        {
            Services.AddSingletonSubscriber(subscriberType);
            return this;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">The subscribers base class or interface.</typeparam>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonSubscriber<TBase, TSubscriber>()
            where TSubscriber : class, ISubscriber
        {
            Services.AddSingletonSubscriber<TBase, TSubscriber>();
            return this;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonSubscriber<TSubscriber>()
            where TSubscriber : class, ISubscriber
        {
            Services.AddSingletonSubscriber<TSubscriber>();
            return this;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
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
            Services.AddSingletonSubscriber(baseType, subscriberType, implementationFactory);
            return this;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="subscriberType">The type of the subscriber to register.</param>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonSubscriber(
            Type subscriberType,
            Func<IServiceProvider, ISubscriber> implementationFactory)
        {
            Services.AddSingletonSubscriber(subscriberType, implementationFactory);
            return this;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">The subscribers base class or interface.</typeparam>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonSubscriber<TBase, TSubscriber>(
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber
        {
            Services.AddSingletonSubscriber<TBase, TSubscriber>(implementationFactory);
            return this;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TSubscriber">The type of the subscriber to add.</typeparam>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonSubscriber<TSubscriber>(
            Func<IServiceProvider, TSubscriber> implementationFactory)
            where TSubscriber : class, ISubscriber
        {
            Services.AddSingletonSubscriber(implementationFactory);
            return this;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> with an
        ///     instance specified in <paramref name="implementationInstance" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
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
            Services.AddSingletonSubscriber(baseType, subscriberType, implementationInstance);
            return this;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <paramref name="subscriberType" /> with an
        ///     instance specified in <paramref name="implementationInstance" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="subscriberType">The type of the subscriber to register.</param>
        /// <param name="implementationInstance">The instance of the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonSubscriber(Type subscriberType, ISubscriber implementationInstance)
        {
            Services.AddSingletonSubscriber(subscriberType, implementationInstance);
            return this;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> with an
        ///     instance specified in <paramref name="implementationInstance" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBase">The subscribers base class or interface.</typeparam>
        /// <typeparam name="TSubscriber">The type of the subscriber to register.</typeparam>
        /// <param name="implementationInstance">The instance of the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonSubscriber<TBase, TSubscriber>(TSubscriber implementationInstance)
            where TSubscriber : class, ISubscriber
        {
            Services.AddSingletonSubscriber(implementationInstance);
            return this;
        }

        /// <summary>
        ///     Adds a singleton subscriber of the type specified in <typeparamref name="TSubscriber" /> with an
        ///     instance specified in <paramref name="implementationInstance" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="implementationInstance">The instance of the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonSubscriber<TSubscriber>(TSubscriber implementationInstance)
            where TSubscriber : class, ISubscriber
        {
            Services.AddSingletonSubscriber(implementationInstance);
            return this;
        }

        #endregion

        #endregion

        #region Behaviors

        #region AddTransientBehavior

        /// <summary>
        ///     Adds a scoped behavior of the type specified in <paramref name="behaviorType" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="behaviorType">The type of the behavior to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddTransientBehavior(Type behaviorType)
        {
            Services.AddTransientBehavior(behaviorType);
            return this;
        }

        /// <summary>
        ///     Adds a scoped behavior of the type specified in <typeparamref name="TBehavior" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBehavior">The type of the behavior to add.</typeparam>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddTransientBehavior<TBehavior>()
            where TBehavior : class, IBehavior
        {
            Services.AddTransientBehavior<TBehavior>();
            return this;
        }

        /// <summary>
        ///     Adds a scoped behavior with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddTransientBehavior(Func<IServiceProvider, IBehavior> implementationFactory)
        {
            Services.AddTransientBehavior(implementationFactory);
            return this;
        }

        #endregion

        #region AddScopedBehavior

        /// <summary>
        ///     Adds a scoped behavior of the type specified in <paramref name="behaviorType" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="behaviorType">The type of the behavior to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddScopedBehavior(Type behaviorType)
        {
            Services.AddScopedBehavior(behaviorType);
            return this;
        }

        /// <summary>
        ///     Adds a scoped behavior of the type specified in <typeparamref name="TBehavior" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBehavior">The type of the behavior to add.</typeparam>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddScopedBehavior<TBehavior>()
            where TBehavior : class, IBehavior
        {
            Services.AddScopedBehavior<TBehavior>();
            return this;
        }

        /// <summary>
        ///     Adds a scoped behavior with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddScopedBehavior(Func<IServiceProvider, IBehavior> implementationFactory)
        {
            Services.AddScopedBehavior(implementationFactory);
            return this;
        }

        #endregion

        #region AddSingletonBehavior

        /// <summary>
        ///     Adds a singleton behavior of the type specified in <paramref name="behaviorType" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="behaviorType">The type of the behavior to register and the implementation to use.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonBehavior(Type behaviorType)
        {
            Services.AddSingletonBehavior(behaviorType);
            return this;
        }

        /// <summary>
        ///     Adds a singleton behavior of the type specified in <typeparamref name="TBehavior" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <typeparam name="TBehavior">The type of the behavior to add.</typeparam>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonBehavior<TBehavior>()
            where TBehavior : class, IBehavior
        {
            Services.AddSingletonBehavior<TBehavior>();
            return this;
        }

        /// <summary>
        ///     Adds a singleton behavior with a
        ///     factory specified in <paramref name="implementationFactory" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="implementationFactory">The factory that creates the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        public ISilverbackBuilder AddSingletonBehavior(Func<IServiceProvider, IBehavior> implementationFactory)
        {
            Services.AddSingletonBehavior(implementationFactory);
            return this;
        }

        /// <summary>
        ///     Adds a singleton behavior with an
        ///     instance specified in <paramref name="implementationInstance" /> to the
        ///     specified <see cref="T:Microsoft.Extensions.DependencyInjection.IServiceCollection" />.
        /// </summary>
        /// <param name="implementationInstance">The instance of the service.</param>
        /// <returns>A reference to this instance after the operation has completed.</returns>
        /// <seealso cref="F:Microsoft.Extensions.DependencyInjection.ServiceLifetime.Singleton" />
        public ISilverbackBuilder AddSingletonBehavior(IBehavior implementationInstance)
        {
            Services.AddSingletonBehavior(implementationInstance);
            return this;
        }

        #endregion

        #endregion
    }
}