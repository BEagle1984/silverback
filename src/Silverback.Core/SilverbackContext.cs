// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Silverback;

/// <summary>
///     Used to persist objects that are valid within the same DI scope. This is used for example to share the storage transaction.
/// </summary>
// TODO: Test
// TODO: Create interface to simplify testing and "hide" SetObject/GetObject -> Define extensions on ISilverbackContext
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
    public void SetObject(int objectTypeId, object obj) => _objects[objectTypeId] = obj;

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
    public bool TryGetObject(int objectTypeId, [NotNullWhen(true)] out object? obj) => _objects.TryGetValue(objectTypeId, out obj);
}
