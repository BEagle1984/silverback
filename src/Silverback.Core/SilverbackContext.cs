// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback;

/// <summary>
///     Used to persist objects that are valid within the same DI scope. This is used for example to share the storage transaction.
/// </summary>
// TODO: Test
public class SilverbackContext
{
    private readonly Dictionary<int, object> _objects = new();

    /// <summary>
    ///     Stores the specified object. If an object with the same type id is already stored, it will be replaced.
    /// </summary>
    /// <param name="objectTypeId">
    ///     An integer number uniquely identifying the object type.
    /// </param>
    /// <param name="obj">
    ///     The object.
    /// </param>
    public void SetObject(int objectTypeId, object obj) =>
        _objects[objectTypeId] = obj;

    // /// <summary>
    // ///     Adds the specified object and will throw an exception if an object with the same type id is already stored.
    // /// </summary>
    // /// <param name="objectTypeId">
    // ///     An integer number uniquely identifying the object type.
    // /// </param>
    // /// <param name="obj">
    // ///     The object.
    // /// </param>
    // public void AddObject(int objectTypeId, object obj) =>
    //     _objects.Add(objectTypeId, obj);

    /// <summary>
    ///     Checks whether an object is set for the specified type and returns it.
    /// </summary>
    /// <param name="objectTypeId">
    ///     An integer number uniquely identifying the object type.
    /// </param>
    /// <param name="obj">
    ///     The object.
    /// </param>
    /// <returns>
    ///     A value indicating whether the transaction was found.
    /// </returns>
    public bool TryGetObject(int objectTypeId, out object? obj) =>
        _objects.TryGetValue(objectTypeId, out obj);
}
