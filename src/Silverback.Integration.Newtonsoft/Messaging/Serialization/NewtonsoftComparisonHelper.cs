// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Newtonsoft.Json;

namespace Silverback.Messaging.Serialization;

internal static class NewtonsoftComparisonHelper
{
    /// <summary>
    ///     Determines whether the specified object instances are considered equal comparing their JSON
    ///     representations.
    /// </summary>
    /// <param name="objA">
    ///     The first of the two objects to be compared.
    /// </param>
    /// <param name="objB">
    ///     The second of the two objects to be compared.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the two objects serializes to the same JSON representation.
    /// </returns>
    public static bool JsonEquals(object? objA, object? objB) =>
        ReferenceEquals(objA, objB) ||
        objA != null &&
        objB != null &&
        string.Equals(
            JsonConvert.SerializeObject(objA),
            JsonConvert.SerializeObject(objB),
            StringComparison.Ordinal);
}
