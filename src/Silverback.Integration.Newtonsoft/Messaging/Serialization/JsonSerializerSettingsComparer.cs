// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Newtonsoft.Json;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Compares the <see cref="JsonSerializerSettings" /> by value.
/// </summary>
/// <remarks>
///     The comparison is based on the fields instead of the properties, because the getters return the default value for the settings
///     that aren't set, while <see cref="JsonSerializer.CreateDefault(JsonSerializerSettings)" /> leaves them to the
///     <see cref="JsonConvert.DefaultSettings" />, and because some obsolete getters have side effects or throw. The value-type and string
///     settings, as well as the delegates, are compared by value and the converters item by item. All other objects, such as the contract
///     resolver, binder and culture, are compared by reference, since their configuration isn't observable. This way a field added by a future version of
///     Newtonsoft.Json is compared as well, and can at worst cause equivalent settings to be considered different.
/// </remarks>
internal static class JsonSerializerSettingsComparer
{
    private static readonly JsonSerializerSettings EmptySettings = new();

    private static readonly FieldInfo[] ComparedFields =
        typeof(JsonSerializerSettings).GetFields(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

    /// <summary>
    ///     Determines whether the specified <see cref="JsonSerializerSettings" /> are equivalent. A <c>null</c> value is considered
    ///     equivalent to a new <see cref="JsonSerializerSettings" /> instance.
    /// </summary>
    /// <param name="x">
    ///     The first settings to compare.
    /// </param>
    /// <param name="y">
    ///     The second settings to compare.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the settings are equivalent, otherwise <c>false</c>.
    /// </returns>
    public static bool AreEqual(JsonSerializerSettings? x, JsonSerializerSettings? y)
    {
        x ??= EmptySettings;
        y ??= EmptySettings;

        if (ReferenceEquals(x, y))
            return true;

        // The fields added by a subclass are unknown
        if (x.GetType() != typeof(JsonSerializerSettings) || y.GetType() != typeof(JsonSerializerSettings))
            return false;

        return ComparedFields.All(field => FieldValuesEqual(field.GetValue(x), field.GetValue(y)));
    }

    private static bool FieldValuesEqual(object? x, object? y)
    {
        if (x is IEnumerable<JsonConverter> convertersX && y is IEnumerable<JsonConverter> convertersY)
            return convertersX.SequenceEqual(convertersY, ReferenceEqualityComparer.Instance);

        // The delegates are equal only when invoking the same method on the same target
        if (x is null || x is string || x is Delegate || x.GetType().IsValueType)
            return Equals(x, y);

        return ReferenceEquals(x, y);
    }
}
