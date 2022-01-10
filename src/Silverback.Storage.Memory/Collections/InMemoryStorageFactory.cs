// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Silverback.Collections;

/// <summary>
///     Being registered as singleton in the IoC container, this class is used as in-memory storage for various items.
/// </summary>
/// <typeparam name="T">
///    The type of the entities.
/// </typeparam>
// public sealed class InMemoryStorage<T>
// {
//     /// <summary>
//     ///     Gets the items stored in memory, wrapped into an <see cref="InMemoryStoredItem{T}"/>.
//     /// </summary>
//     public List<InMemoryStoredItem<T>> Items { get; } = new();
// }

/// <summary>
///     Being registered as singleton in the IoC container, this class is used as in-memory storage for various items.
/// </summary>
/// <typeparam name="T">
///     The type of the entities.
/// </typeparam>
public sealed class InMemoryStorageFactory
{
    private readonly ConcurrentDictionary<object, object> _storages = new();

    /// <summary>
    ///     Gets the shared <see cref="InMemoryStorage{T}"/> according to the specified settings.
    /// </summary>
    public InMemoryStorage<TItems> GetStorage<TSettings, TItems>(TSettings settings)
        where TItems : class =>
        (InMemoryStorage<TItems>)_storages.GetOrAdd(settings, _ => new InMemoryStorage<TItems>());

    /// <summary>
    ///     Gets the items stored in memory, wrapped into an <see cref="InMemoryStoredItem{T}"/>.
    /// </summary>
    //public List<InMemoryStoredItem<TItems>> Items { get; } = new();
}
