// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace Silverback.Util
{
    internal static class TypesCache
    {
        private static readonly ConcurrentDictionary<string, Type?> Cache = new();

        public static Type GetType(string typeName) => GetType(typeName, true)!;

        public static Type? GetType(string typeName, bool throwOnError)
        {
            Check.NotNull(typeName, nameof(typeName));

            var type = Cache.GetOrAdd(typeName, _ => ResolveType(typeName, throwOnError));

            if (throwOnError && type == null)
            {
                type = Cache.AddOrUpdate(
                    typeName,
                    _ => ResolveType(typeName, throwOnError),
                    (_, _) => ResolveType(typeName, throwOnError));
            }

            return type;
        }

        private static string CleanAssemblyQualifiedName(string typeAssemblyQualifiedName)
        {
            if (string.IsNullOrWhiteSpace(typeAssemblyQualifiedName))
                return typeAssemblyQualifiedName;

            var cleanAssemblyQualifiedName = Regex.Replace(typeAssemblyQualifiedName, @", (Version=\d+\.\d+\.\d+\.\d+|Culture=\w+|PublicKeyToken=\w+)", string.Empty);

            return cleanAssemblyQualifiedName;
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
    }
}
