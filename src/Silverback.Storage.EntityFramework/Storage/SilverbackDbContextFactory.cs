// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Data.Common;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Silverback.Storage;

internal static class SilverbackDbContextFactory
{
    private static readonly ConcurrentDictionary<CacheKey, ConstructorInfo> ConstructorsCache = new();

    public static TDbContext CreateDbContext<TDbContext>(IServiceProvider serviceProvider, SilverbackContext? context)
        where TDbContext : DbContext
    {
        DbTransaction? transaction = context?.GetActiveDbTransaction<DbTransaction>();

        return transaction?.Connection != null
            ? (TDbContext)FindConstructorWithDbConnection(typeof(TDbContext), transaction.Connection.GetType()).Invoke(
            [
                transaction.Connection
            ])
            : serviceProvider.GetRequiredService<IDbContextFactory<TDbContext>>().CreateDbContext();
    }

    private static ConstructorInfo FindConstructorWithDbConnection(Type dbContextType, Type connectionType) =>
        ConstructorsCache.GetOrAdd(
            new CacheKey(dbContextType, connectionType),
            static key =>
            {
                ConstructorInfo[] constructors = key.DbContextType.GetConstructors();

                foreach (ConstructorInfo constructor in constructors)
                {
                    ParameterInfo[] parameters = constructor.GetParameters();
                    if (parameters.Length == 1 && parameters[0].ParameterType.IsAssignableFrom(key.ConnectionType))
                    {
                        return constructor;
                    }
                }

                throw new InvalidOperationException($"The type {key.ConnectionType.Name} doesn't have a constructor that accepts a DbConnection.");
            });

    private sealed record CacheKey(Type DbContextType, Type ConnectionType);
}
