// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace Silverback.Util;

/// <summary>
///     Compares the <see cref="JsonSerializerOptions" /> by value.
/// </summary>
/// <remarks>
///     The value-type and string settings are compared by value and the converters item by item. All other objects, such as the type
///     info resolvers, naming policies, encoder and reference handler, are compared by reference, since their configuration isn't
///     observable. The only exception is the default reflection-based resolver, which is implicitly assigned when the options are first
///     used. This way a setting added by a future version of System.Text.Json is compared as well, and can at worst cause equivalent
///     options to be considered different.
/// </remarks>
internal static class JsonSerializerOptionsComparer
{
    private static readonly bool IsSourceGenReflectionFallbackEnabled =
        AppContext.TryGetSwitch("System.Text.Json.Serialization.EnableSourceGenReflectionFallback", out bool isEnabled) && isEnabled;

    private static readonly PropertyInfo[] ComparedProperties =
    [
        .. typeof(JsonSerializerOptions)
            .GetProperties(BindingFlags.Instance | BindingFlags.Public)
            .Where(
                property => property.GetIndexParameters().Length == 0 &&
                            property.Name is not (nameof(JsonSerializerOptions.IsReadOnly) or
                                nameof(JsonSerializerOptions.TypeInfoResolver) or
                                nameof(JsonSerializerOptions.TypeInfoResolverChain)))
    ];

    /// <summary>
    ///     Determines whether the specified <see cref="JsonSerializerOptions" /> are equivalent. A <c>null</c> value is considered
    ///     equivalent to the <see cref="JsonSerializerOptions.Default" /> options.
    /// </summary>
    /// <param name="x">
    ///     The first options to compare.
    /// </param>
    /// <param name="y">
    ///     The second options to compare.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the options are equivalent, otherwise <c>false</c>.
    /// </returns>
    public static bool AreEqual(JsonSerializerOptions? x, JsonSerializerOptions? y)
    {
        x ??= JsonSerializerOptions.Default;
        y ??= JsonSerializerOptions.Default;

        if (ReferenceEquals(x, y))
            return true;

        return ComparedProperties.All(property => PropertyValuesEqual(property.GetValue(x), property.GetValue(y))) &&
               GetEffectiveResolvers(x).SequenceEqual(GetEffectiveResolvers(y), ResolverEqualityComparer.Instance);
    }

    // With the .NET 6 compatibility switch enabled, a context set directly as TypeInfoResolver (not in a chain) falls back to reflection
    private static IEnumerable<IJsonTypeInfoResolver?> GetEffectiveResolvers(JsonSerializerOptions options) =>
        IsSourceGenReflectionFallbackEnabled && options.TypeInfoResolver is JsonSerializerContext context
            ? [context, JsonSerializerOptions.Default.TypeInfoResolver]
            : GetResolvers(options.TypeInfoResolver);

    private static bool PropertyValuesEqual(object? x, object? y)
    {
        if (x is IEnumerable<JsonConverter> convertersX && y is IEnumerable<JsonConverter> convertersY)
            return convertersX.SequenceEqual(convertersY, ReferenceEqualityComparer.Instance);

        if (x is null || x is string || x.GetType().IsValueType)
            return Equals(x, y);

        return ReferenceEquals(x, y);
    }

    // The TypeInfoResolver is the effective resolver: it returns the chain itself when the chain is used or when combining resolvers.
    // The chains are flattened recursively, since a combined resolver added to the chain is stored as a single nested item. Only the
    // System.Text.Json chains are flattened, because a custom enumerable resolver could combine its items differently.
    [SuppressMessage("ReSharper", "SuspiciousTypeConversion.Global", Justification = "The resolver chains are internal System.Text.Json types implementing IEnumerable<IJsonTypeInfoResolver>")]
    private static IEnumerable<IJsonTypeInfoResolver?> GetResolvers(IJsonTypeInfoResolver? resolver)
    {
        if (resolver is IEnumerable<IJsonTypeInfoResolver> chain && resolver.GetType().Assembly == typeof(IJsonTypeInfoResolver).Assembly)
            return chain.SelectMany(GetResolvers);

        return [resolver];
    }

    private sealed class ResolverEqualityComparer : IEqualityComparer<IJsonTypeInfoResolver?>
    {
        public static ResolverEqualityComparer Instance { get; } = new();

        public bool Equals(IJsonTypeInfoResolver? x, IJsonTypeInfoResolver? y) =>
            ReferenceEquals(x, y) || IsDefaultResolver(x) && IsDefaultResolver(y);

        public int GetHashCode(IJsonTypeInfoResolver? obj) => IsDefaultResolver(obj) ? 0 : RuntimeHelpers.GetHashCode(obj);

        // A missing resolver is replaced with the default reflection-based one when the options are used, if reflection is enabled
        private static bool IsDefaultResolver(IJsonTypeInfoResolver? resolver) =>
            resolver == null
                ? JsonSerializer.IsReflectionEnabledByDefault
                : ReferenceEquals(resolver, JsonSerializerOptions.Default.TypeInfoResolver);
    }
}
