// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Silverback.Lock;
using Silverback.Storage;

namespace Silverback.Configuration;

/// <summary>
///     Adds the <see cref="UseDbContext{TDbContext}" /> methods to the <see cref="DistributedLockSettingsBuilder" />.
/// </summary>
public static class DistributedLockSettingsBuilderEntityFrameworkExtensions
{
    /// <summary>
    ///     Configures the Entity Framework based lock.
    /// </summary>
    /// <typeparam name="TDbContext">
    ///     The type of the <see cref="DbContext" />.
    /// </typeparam>
    /// <param name="builder">
    ///     The <see cref="DistributedLockSettingsBuilder" />.
    /// </param>
    /// <param name="lockName">
    ///     The lock name.
    /// </param>
    /// <returns>
    ///     The <see cref="EntityFrameworkLockSettingsBuilder" />.
    /// </returns>
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Extension method (fluent API)")]
    public static EntityFrameworkLockSettingsBuilder UseDbContext<TDbContext>(
        this DistributedLockSettingsBuilder builder,
        string lockName)
        where TDbContext : DbContext =>
        new(lockName, typeof(TDbContext), SilverbackDbContextFactory.CreateDbContext<TDbContext>);
}
