// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Silverback.Util;

#if NETSTANDARD
internal static class TypesCache
#else
internal static partial class TypesCache
#endif
{
    private static readonly ConcurrentDictionary<string, Type?> Cache = new();

    public static Type? GetType(string? typeName, bool throwOnError = true)
    {
        if (string.IsNullOrEmpty(typeName))
            return null;

        Type? type = Cache.GetOrAdd(
            typeName,
            static (factoryTypeName, factoryThrowOnError) => ResolveType(factoryTypeName, factoryThrowOnError),
            throwOnError);

        if (throwOnError && type == null)
        {
            type = Cache.AddOrUpdate(
                typeName,
                static (factoryTypeName, factoryThrowOnError) => ResolveType(factoryTypeName, factoryThrowOnError),
                static (factoryTypeName, _, factoryThrowOnError) => ResolveType(factoryTypeName, factoryThrowOnError),
                throwOnError);
        }

        return type;
    }

    [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Can catch all, the operation is retried")]
    private static Type? ResolveType(string typeName, bool throwOnError)
    {
        Type? type = null;

        try
        {
            type = Type.GetType(typeName);
        }
        catch
        {
            // Ignored
        }

        type ??= Type.GetType(CleanAssemblyQualifiedName(typeName), throwOnError);

        return type;
    }

    private static string CleanAssemblyQualifiedName(string typeAssemblyQualifiedName)
    {
        if (string.IsNullOrWhiteSpace(typeAssemblyQualifiedName))
            return typeAssemblyQualifiedName;

#if NETSTANDARD
        string cleanAssemblyQualifiedName = Regex.Replace(typeAssemblyQualifiedName, @", (Version=\d+\.\d+\.\d+\.\d+|Culture=\w+|PublicKeyToken=\w+)", string.Empty);
#else
        string cleanAssemblyQualifiedName = CleanAssemblyQualifiedNameRegex().Replace(typeAssemblyQualifiedName, string.Empty);
#endif

        return cleanAssemblyQualifiedName;
    }

#if !NETSTANDARD
    [GeneratedRegex(@", (Version=\d+\.\d+\.\d+\.\d+|Culture=\w+|PublicKeyToken=\w+)")]
    private static partial Regex CleanAssemblyQualifiedNameRegex();
#endif
}
