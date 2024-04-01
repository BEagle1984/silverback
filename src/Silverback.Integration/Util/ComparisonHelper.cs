// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text.Json;

namespace Silverback.Util;

internal static class ComparisonHelper
{
    private static readonly JsonSerializerOptions JsonSerializerOptions = new()
    {
        MaxDepth = 1000
    };

    /// <summary>
    ///     Determines whether the specified object instances are considered equal comparing their JSON representations.
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
        string.Equals(GetJsonString(objA), GetJsonString(objB), StringComparison.Ordinal);

    private static string GetJsonString(object obj) =>
        JsonSerializer.Serialize(
            obj,
            obj.GetType(),
            JsonSerializerOptions);
}
