// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Silverback.ExtensibleFactories;

/// <summary>
///     The base class for factories used to allow extension in additional packages, for example adding other storage types.
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
/// <typeparam name="TSettingsBase">
///     The base type of the settings to use.
/// </typeparam>
public abstract class ExtensibleFactory<TService, TSettingsBase> : IExtensibleFactory
    where TService : notnull
    where TSettingsBase : IEquatable<TSettingsBase>
{
    private readonly Dictionary<Type, Func<TSettingsBase, IServiceProvider, TService>> _factories = new();

    private readonly ConcurrentDictionary<TSettingsBase, TService> _cache = new();

    private Func<TSettingsBase, IServiceProvider, TService>? _overrideFactory;

    /// <summary>
    ///     Registers the factory for the specified settings type.
    /// </summary>
    /// <typeparam name="TSettings">
    ///     The type of the settings.
    /// </typeparam>
    /// <param name="factory">
    ///     The factory building the <typeparamref name="TService" /> according to the specified settings.
    /// </param>
    public virtual void AddFactory<TSettings>(Func<TSettings, IServiceProvider, TService> factory)
        where TSettings : TSettingsBase
    {
        if (_factories.ContainsKey(typeof(TSettings)))
            throw new InvalidOperationException("The factory for the specified settings type is already registered.");

        _factories.Add(typeof(TSettings), (settings, serviceProvider) => factory.Invoke((TSettings)settings, serviceProvider));
    }

    /// <summary>
    ///     Returns a boolean value indicating whether a factory for the specified settings type is registered.
    /// </summary>
    /// <typeparam name="TSettings">
    ///     The type of the settings.
    /// </typeparam>
    /// <returns>
    ///     A value indicating whether a factory for the specified settings type is registered.
    /// </returns>
    public bool HasFactory<TSettings>() => _factories.ContainsKey(typeof(TSettings));

    /// <summary>
    ///     Overrides all registered factories with the specified factory.
    /// </summary>
    /// <param name="factory">
    ///     The factory to be used regardless of the settings type.
    /// </param>
    public void OverrideFactories(Func<TSettingsBase, IServiceProvider, TService> factory) => _overrideFactory = factory;

    /// <summary>
    ///     Returns an object of type <typeparamref name="TService" /> according to the specified settings.
    /// </summary>
    /// <typeparam name="TSettings">
    ///     The type of the settings.
    /// </typeparam>
    /// <param name="settings">
    ///     The settings that will be used to create the service.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> that can be used to resolve additional services.
    /// </param>
    /// <returns>
    ///     The service of type <typeparamref name="TService" />, or <c>null</c> if no factory is registered for the specified settings type.
    /// </returns>
    protected TService GetService<TSettings>(TSettings settings, IServiceProvider serviceProvider)
        where TSettings : TSettingsBase =>
        _cache.GetOrAdd(
            settings,
            static (_, args) =>
            {
                Type settingsType = args.Settings.GetType();

                if (args.OverrideFactory != null)
                    return args.OverrideFactory.Invoke(args.Settings, args.ServiceProvider);

                if (args.Factories.TryGetValue(settingsType, out Func<TSettingsBase, IServiceProvider, TService>? factory))
                    return factory.Invoke(args.Settings, args.ServiceProvider);

                throw new InvalidOperationException($"No factory registered for the specified settings type ({settingsType.Name}).");
            },
            (Settings: settings, ServiceProvider: serviceProvider, OverrideFactory: _overrideFactory, Factories: _factories));
}
