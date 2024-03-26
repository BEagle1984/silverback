// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using Silverback.Util;

namespace Silverback.ExtensibleFactories;

/// <summary>
///     The base class for factories used to allow extension in additional packages, for example adding broker-specific extensions.
/// </summary>
/// <remarks>
///     Two versions are available:
///     <list type="bullet">
///         <item>
///             <description>
///                 <see cref="TypeBasedExtensibleFactory{TService,TDiscriminatorBase}" />, using just a type as discriminator
///             </description>
///         </item>
///         <item>
///             <description>
///                 <see cref="ExtensibleFactory{TService,TDiscriminatorBase}" />, using a settings record as discriminator
///             </description>
///         </item>
///     </list>
/// </remarks>
/// <typeparam name="TService">
///     The type of the service to build.
/// </typeparam>
/// <typeparam name="TDiscriminatorBase">
///     The discriminator base type.
/// </typeparam>
public abstract class TypeBasedExtensibleFactory<TService, TDiscriminatorBase> : ITypeBasedExtensibleFactory
    where TService : notnull
    where TDiscriminatorBase : IEquatable<TDiscriminatorBase>
{
    private readonly Dictionary<Type, Func<IServiceProvider, TService>> _factories = new();

    private readonly ConcurrentDictionary<Type, TService?>? _cache;

    private Func<IServiceProvider, TService>? _overrideFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="TypeBasedExtensibleFactory{TService, TDiscriminatorBase}" /> class.
    /// </summary>
    /// <param name="cacheInstances">
    ///     A value indicating whether the instances should be cached. If <c>true</c> the same instance will be returned for the same
    ///     discriminator type.
    /// </param>
    protected TypeBasedExtensibleFactory(bool cacheInstances = true)
    {
        if (cacheInstances)
            _cache = new ConcurrentDictionary<Type, TService?>();
    }

    /// <summary>
    ///     Registers the factory for the specified discriminator implementation type.
    /// </summary>
    /// <typeparam name="TDiscriminator">
    ///     The discriminator implementation type.
    /// </typeparam>
    /// <param name="factory">
    ///     The factory building the <typeparamref name="TService" /> according to the specified discriminator.
    /// </param>
    public virtual void AddFactory<TDiscriminator>(Func<IServiceProvider, TService> factory)
        where TDiscriminator : TDiscriminatorBase
    {
        if (_factories.ContainsKey(typeof(TDiscriminator)))
            throw new InvalidOperationException("The factory for the specified discriminator type is already registered.");

        _factories.Add(typeof(TDiscriminator), factory);
    }

    /// <summary>
    ///     Returns a boolean value indicating whether a factory for the specified discriminator type is registered.
    /// </summary>
    /// <typeparam name="TDiscriminator">
    ///     The discriminator implementation type.
    /// </typeparam>
    /// <returns>
    ///     A value indicating whether a factory for the specified discriminator type is registered.
    /// </returns>
    public bool HasFactory<TDiscriminator>() => _factories.ContainsKey(typeof(TDiscriminator));

    /// <summary>
    ///     Overrides all registered factories with the specified factory.
    /// </summary>
    /// <param name="factory">
    ///     The factory to be used regardless of the discriminator type.
    /// </param>
    public void OverrideFactories(Func<IServiceProvider, TService> factory) => _overrideFactory = factory;

    /// <summary>
    ///     Returns an object of type <typeparamref name="TService" /> according to the specified discriminator.
    /// </summary>
    /// <param name="discriminator">
    ///     The discriminator.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> that can be used to resolve additional services.
    /// </param>
    /// <returns>
    ///     The service of type <typeparamref name="TService" />, or <c>null</c> if no factory is registered for the specified
    ///     discriminator type.
    /// </returns>
    protected TService? GetService(TDiscriminatorBase discriminator, IServiceProvider serviceProvider) =>
        GetService(Check.NotNull(discriminator, nameof(discriminator)).GetType(), serviceProvider);

    /// <summary>
    ///     Returns an object of type <typeparamref name="TService" /> according to the specified discriminator.
    /// </summary>
    /// <param name="discriminatorType">
    ///     The discriminator implementation type.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> that can be used to resolve additional services.
    /// </param>
    /// <returns>
    ///     The service of type <typeparamref name="TService" />, or <c>null</c> if no factory is registered for the specified
    ///     discriminator type.
    /// </returns>
    protected TService? GetService(Type discriminatorType, IServiceProvider serviceProvider) =>
        _cache == null
            ? CreateServiceInstance(discriminatorType, _factories, _overrideFactory, serviceProvider)
            : _cache.GetOrAdd(
                discriminatorType,
                static (_, args) =>
                    CreateServiceInstance(args.DiscriminatorType, args.Factories, args.OverrideFactory, args.ServiceProvider),
                (DiscriminatorType: discriminatorType, ServiceProvider: serviceProvider, Factories: _factories, OverrideFactory: _overrideFactory));

    private static TService? CreateServiceInstance(
        Type discriminatorType,
        Dictionary<Type, Func<IServiceProvider, TService>> factories,
        Func<IServiceProvider, TService>? overrideFactory,
        IServiceProvider serviceProvider)
    {
        if (overrideFactory != null)
            return overrideFactory.Invoke(serviceProvider);

        if (factories.TryGetValue(discriminatorType, out Func<IServiceProvider, TService>? factory))
            return factory.Invoke(serviceProvider);

        return default;
    }
}
