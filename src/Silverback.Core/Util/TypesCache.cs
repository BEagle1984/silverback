// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Util;

internal static class TypesCache
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

    [SuppressMessage("", "CA1031", Justification = "Can catch all, the operation is retried")]
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
        if (string.IsNullOrEmpty(typeAssemblyQualifiedName))
            return typeAssemblyQualifiedName;

        string[] split = typeAssemblyQualifiedName.Split(',');

        return split.Length >= 2 ? $"{split[0]}, {split[1]}" : typeAssemblyQualifiedName;
    }
}
