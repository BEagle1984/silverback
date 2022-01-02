// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Silverback.Lock;

/// <inheritdoc cref="IDistributedLockFactory" />
public class DistributedLockFactory : IDistributedLockFactory
{
    private readonly Dictionary<Type, Func<DistributedLockSettings, DistributedLock>> _factories = new();

    private readonly Dictionary<Type, ConcurrentDictionary<string, IDistributedLock>> _cache = new();

    private Func<DistributedLockSettings, DistributedLock>? _overrideFactory;

    /// <inheritdoc cref="IDistributedLockFactory.GetDistributedLock{TSettings}" />
    public IDistributedLock GetDistributedLock<TSettings>(TSettings? settings)
        where TSettings : DistributedLockSettings
    {
        if (settings == null)
            return NullLock.Instance;

        if (!_cache.TryGetValue(typeof(TSettings), out ConcurrentDictionary<string, IDistributedLock>? cache))
            throw new InvalidOperationException($"No factory registered for the specified settings type ({typeof(TSettings).Name}).");

        return cache.GetOrAdd(
            settings.LockName,
            static (_, args) =>
                args._overrideFactory == null
                    ? args._factories[args.settings.GetType()].Invoke(args.settings)
                    : args._overrideFactory.Invoke(args.settings),
            (settings, _overrideFactory, _factories));
    }

    /// <summary>
    ///     Registers the factory for the specified settings type.
    /// </summary>
    /// <typeparam name="TSettings">
    ///     The type of the <see cref="DistributedLockSettings" />.
    /// </typeparam>
    /// <param name="factory">
    ///     The factory building the <see cref="IDistributedLock" /> according to the specified settings.
    /// </param>
    public void AddFactory<TSettings>(Func<TSettings, DistributedLock> factory)
        where TSettings : DistributedLockSettings
    {
        if (_factories.ContainsKey(typeof(TSettings)))
            throw new InvalidOperationException("The factory for the specified settings type is already registered.");

        _factories.Add(typeof(TSettings), settings => factory((TSettings)settings));
        _cache.Add(typeof(TSettings), new ConcurrentDictionary<string, IDistributedLock>());
    }

    /// <summary>
    ///     Returns a boolean value indicating whether a factory for the specified settings type is registered.
    /// </summary>
    /// <typeparam name="TSettings">
    ///     The type of the <see cref="DistributedLockSettings" />.
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
    public void OverrideFactories(Func<DistributedLockSettings, DistributedLock> factory) => _overrideFactory = factory;
}
