// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Silverback.Storage;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <seealso cref="UseEntityFramework{TDbContext}" /> method to the <see cref="OutboxSettingsBuilder" />.
/// </summary>
public static class OutboxSettingsBuilderEntityFrameworkExtensions
{
    /// <summary>
    ///     Configures the Entity Framework based outbox.
    /// </summary>
    /// <typeparam name="TDbContext">
    ///     The type of the <see cref="DbContext" />.
    /// </typeparam>
    /// <param name="builder">
    ///     The <see cref="OutboxSettingsBuilder" />.
    /// </param>
    /// <returns>
    ///     The <see cref="EntityFrameworkOutboxSettingsBuilder" />.
    /// </returns>
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Extension method (fluent API)")]
    public static EntityFrameworkOutboxSettingsBuilder UseEntityFramework<TDbContext>(this OutboxSettingsBuilder builder)
        where TDbContext : DbContext =>
        new(typeof(TDbContext), SilverbackDbContextFactory.CreateDbContext<TDbContext>);
}
