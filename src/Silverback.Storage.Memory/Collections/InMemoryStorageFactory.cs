// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;

namespace Silverback.Collections;

/// <summary>
///     Being registered as singleton in the IoC container, this class is used as in-memory storage for various items.
/// </summary>
public sealed class InMemoryStorageFactory
{
    private readonly ConcurrentDictionary<object, object> _storages = new();

    /// <summary>
    ///     Gets the shared <see cref="InMemoryStorage{T}" /> according to the specified settings.
    /// </summary>
    /// <typeparam name="TSettings">
    ///     The type of the settings.
    /// </typeparam>
    /// <typeparam name="TItems">
    ///     The type of the stored items.
    /// </typeparam>
    /// <param name="settings">
    ///     The storage settings.
    /// </param>
    /// <returns>
    ///     The <see cref="InMemoryStorage{T}" />.
    /// </returns>
    public InMemoryStorage<TItems> GetStorage<TSettings, TItems>(TSettings settings)
        where TSettings : class
        where TItems : class =>
        (InMemoryStorage<TItems>)_storages.GetOrAdd(settings, _ => new InMemoryStorage<TItems>());
}
