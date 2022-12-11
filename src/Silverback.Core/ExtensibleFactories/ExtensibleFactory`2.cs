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
    private readonly Dictionary<Type, Func<TSettingsBase, TService>> _factories = new();

    private readonly ConcurrentDictionary<TSettingsBase, TService> _cache = new();

    private Func<TSettingsBase, TService>? _overrideFactory;

    /// <summary>
    ///     Registers the factory for the specified settings type.
    /// </summary>
    /// <typeparam name="TSettings">
    ///     The type of the settings.
    /// </typeparam>
    /// <param name="factory">
    ///     The factory building the <typeparamref name="TService" /> according to the specified settings.
    /// </param>
    public virtual void AddFactory<TSettings>(Func<TSettings, TService> factory)
        where TSettings : TSettingsBase
    {
        if (_factories.ContainsKey(typeof(TSettings)))
            throw new InvalidOperationException("The factory for the specified settings type is already registered.");

        _factories.Add(typeof(TSettings), settings => factory.Invoke((TSettings)settings));
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
    public void OverrideFactories(Func<TSettingsBase, TService> factory) => _overrideFactory = factory;

    /// <summary>
    ///     Returns an object of type <typeparamref name="TService" /> according to the specified settings.
    /// </summary>
    /// <typeparam name="TSettings">
    ///     The type of the settings.
    /// </typeparam>
    /// <param name="settings">
    ///     The settings that will be used to create the service.
    /// </param>
    /// <returns>
    ///     The service of type <typeparamref name="TService" />, or <c>null</c> if no factory is registered for the specified settings type.
    /// </returns>
    protected TService GetService<TSettings>(TSettings settings)
        where TSettings : TSettingsBase =>
        _cache.GetOrAdd(
            settings,
            static (_, args) =>
            {
                Type settingsType = args.Settings.GetType();

                if (args.OverrideFactory != null)
                    return args.OverrideFactory.Invoke(args.Settings);

                if (args.Factories.TryGetValue(settingsType, out Func<TSettingsBase, TService>? factory))
                    return factory.Invoke(args.Settings);

                throw new InvalidOperationException($"No factory registered for the specified settings type ({settingsType.Name}).");
            },
            (Settings: settings, OverrideFactory: _overrideFactory, Factories: _factories));
}
