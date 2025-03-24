// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;

namespace Silverback;

/// <summary>
///     Used to persist objects that are valid within the same DI scope. This is used for example to share the storage transaction.
/// </summary>
public interface ISilverbackContext
{
    /// <summary>
    ///     Gets the <see cref="IServiceProvider" /> used in the current scope.
    /// </summary>
    IServiceProvider ServiceProvider { get; }

    /// <summary>
    ///     Stores the specified object. It will throw if an object with the same type id is already stored.
    /// </summary>
    /// <param name="objectTypeId">
    ///     A unique identifier for the object type.
    /// </param>
    /// <param name="obj">
    ///     The object.
    /// </param>
    void AddObject(Guid objectTypeId, object obj);

    /// <summary>
    ///     Stores the specified object. If an object with the same type id is already stored, it will be replaced.
    /// </summary>
    /// <param name="objectTypeId">
    ///     A unique identifier for the object type.
    /// </param>
    /// <param name="obj">
    ///     The object.
    /// </param>
    void SetObject(Guid objectTypeId, object obj);

    /// <summary>
    ///     Removes the object with the specified type id.
    /// </summary>
    /// <param name="objectTypeId">
    ///     A unique identifier for the object type.
    /// </param>
    /// <returns>
    ///     A value indicating whether the object was found and removed.
    /// </returns>
    bool RemoveObject(Guid objectTypeId);

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
    T GetObject<T>(Guid objectTypeId);

    /// <summary>
    ///     Returns the object with the specified type id.
    /// </summary>
    /// <param name="objectTypeId">
    ///     A unique identifier for the object type.
    /// </param>
    /// <returns>
    ///     A value indicating whether the transaction was found.
    /// </returns>
    object GetObject(Guid objectTypeId);

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
    bool TryGetObject<T>(Guid objectTypeId, [NotNullWhen(true)] out T? obj);

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
    bool TryGetObject(Guid objectTypeId, [NotNullWhen(true)] out object? obj);

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
    T GetOrAddObject<T>(Guid objectTypeId, Func<T> factory);

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
    TObject GetOrAddObject<TObject, TArg>(Guid objectTypeId, Func<TArg, TObject> factory, TArg argument);
}
