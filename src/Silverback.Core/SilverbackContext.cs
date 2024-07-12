// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Util;

namespace Silverback;

/// <summary>
///     Used to persist objects that are valid within the same DI scope. This is used for example to share the storage transaction.
/// </summary>
public class SilverbackContext
{
    private readonly Dictionary<Guid, object> _objects = [];

    /// <summary>
    ///     Initializes a new instance of the <see cref="SilverbackContext" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" />.
    /// </param>
    public SilverbackContext(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    internal IServiceProvider ServiceProvider { get; }

    /// <summary>
    ///     Stores the specified object. It will throw if an object with the same type id is already stored.
    /// </summary>
    /// <param name="objectTypeId">
    ///     A unique identifier for the object type.
    /// </param>
    /// <param name="obj">
    ///     The object.
    /// </param>
    public void AddObject(Guid objectTypeId, object obj)
    {
        if (!_objects.TryAdd(objectTypeId, obj) && GetObject(objectTypeId) != obj)
            throw new InvalidOperationException($"An object of type {objectTypeId} has already been added.");
    }

    /// <summary>
    ///     Stores the specified object. If an object with the same type id is already stored, it will be replaced.
    /// </summary>
    /// <param name="objectTypeId">
    ///     A unique identifier for the object type.
    /// </param>
    /// <param name="obj">
    ///     The object.
    /// </param>
    public void SetObject(Guid objectTypeId, object obj) => _objects[objectTypeId] = obj;

    /// <summary>
    ///     Removes the object with the specified type id.
    /// </summary>
    /// <param name="objectTypeId">
    ///     A unique identifier for the object type.
    /// </param>
    /// <returns>
    ///     A value indicating whether the object was found and removed.
    /// </returns>
    public bool RemoveObject(Guid objectTypeId) => _objects.Remove(objectTypeId);

    /// <summary>
    ///     Returns the object with the specified type id.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the object.
    /// </typeparam>
    /// <param name="objectTypeId">
    ///     A unique identifier for the object type.
    /// </param>
    /// <returns>
    ///     A value indicating whether the transaction was found.
    /// </returns>
    public T GetObject<T>(Guid objectTypeId) => TryGetObject(objectTypeId, out T? obj)
        ? obj
        : throw new InvalidOperationException($"The object with type id {objectTypeId} was not found.");

    /// <summary>
    ///     Returns the object with the specified type id.
    /// </summary>
    /// <param name="objectTypeId">
    ///     A unique identifier for the object type.
    /// </param>
    /// <returns>
    ///     A value indicating whether the transaction was found.
    /// </returns>
    public object GetObject(Guid objectTypeId) => TryGetObject(objectTypeId, out object? obj)
        ? obj
        : throw new InvalidOperationException($"The object with type id {objectTypeId} was not found.");

    /// <summary>
    ///     Checks whether an object is set for the specified type and returns it.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the object.
    /// </typeparam>
    /// <param name="objectTypeId">
    ///     A unique identifier for the object type.
    /// </param>
    /// <param name="obj">
    ///     The object.
    /// </param>
    /// <returns>
    ///     A value indicating whether the transaction was found.
    /// </returns>
    public bool TryGetObject<T>(Guid objectTypeId, [NotNullWhen(true)] out T? obj)
    {
        if (TryGetObject(objectTypeId, out object? tmpObj))
        {
            if (tmpObj is not T typedObj)
                throw new InvalidOperationException($"The object with type id {objectTypeId} is not of type {typeof(T)}.");

            obj = typedObj;
            return true;
        }

        obj = default;
        return false;
    }

    /// <summary>
    ///     Checks whether an object is set for the specified type and returns it.
    /// </summary>
    /// <param name="objectTypeId">
    ///     A unique identifier for the object type.
    /// </param>
    /// <param name="obj">
    ///     The object.
    /// </param>
    /// <returns>
    ///     A value indicating whether the transaction was found.
    /// </returns>
    public bool TryGetObject(Guid objectTypeId, [NotNullWhen(true)] out object? obj) => _objects.TryGetValue(objectTypeId, out obj);

    /// <summary>
    ///     Returns the object with the specified type id or adds a new one if not found.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the object.
    /// </typeparam>
    /// <param name="objectTypeId">
    ///     A unique identifier for the object type.
    /// </param>
    /// <param name="factory">
    ///     The factory to create the object if not found.
    /// </param>
    /// <returns>
    ///     The object.
    /// </returns>
    public T GetOrAddObject<T>(Guid objectTypeId, Func<T> factory)
    {
        Check.NotNull(factory, nameof(factory));

        if (TryGetObject(objectTypeId, out T? obj))
            return obj;

        T newObj = factory.Invoke() ?? throw new InvalidOperationException("The factory returned null.");
        AddObject(objectTypeId, newObj);
        return newObj;
    }

    /// <summary>
    ///     Returns the object with the specified type id or adds a new one if not found.
    /// </summary>
    /// <typeparam name="TObject">
    ///     The type of the object.
    /// </typeparam>
    /// <typeparam name="TArg">
    ///     The type of the argument to pass to the factory.
    /// </typeparam>
    /// <param name="objectTypeId">
    ///     A unique identifier for the object type.
    /// </param>
    /// <param name="factory">
    ///     The factory to create the object if not found.
    /// </param>
    /// <param name="argument">
    ///     The argument to pass to the factory.
    /// </param>
    /// <returns>
    ///     The object.
    /// </returns>
    public TObject GetOrAddObject<TObject, TArg>(Guid objectTypeId, Func<TArg, TObject> factory, TArg argument)
    {
        Check.NotNull(factory, nameof(factory));

        if (TryGetObject(objectTypeId, out TObject? obj))
            return obj;

        TObject newObj = factory.Invoke(argument) ?? throw new InvalidOperationException("The factory returned null.");
        AddObject(objectTypeId, newObj);
        return newObj;
    }
}
