// Copyright (c) 2024 Sergio Aquilini
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

    internal static string CleanAssemblyQualifiedName(string typeAssemblyQualifiedName)
    {
        if (string.IsNullOrEmpty(typeAssemblyQualifiedName))
            return typeAssemblyQualifiedName;

        int endGenericType = typeAssemblyQualifiedName.LastIndexOf(']');
        if (endGenericType == -1)
        {
            string[] split = typeAssemblyQualifiedName.Split(',', 3, StringSplitOptions.RemoveEmptyEntries);
            return split.Length >= 2 ? $"{split[0].Trim()}, {split[1].Trim()}" : typeAssemblyQualifiedName;
        }

        int startGenericType = typeAssemblyQualifiedName.IndexOf('[', StringComparison.InvariantCulture);
        if (startGenericType == -1)
            return typeAssemblyQualifiedName;

        string type = typeAssemblyQualifiedName[..startGenericType].Trim();
        if (endGenericType + 1 >= typeAssemblyQualifiedName.Length)
            return type;

        string next = typeAssemblyQualifiedName[(endGenericType + 1)..];
        string assemblyName = next.Split(",", 2, StringSplitOptions.RemoveEmptyEntries)[0].Trim();
        return $"{type}, {assemblyName}";
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
}
