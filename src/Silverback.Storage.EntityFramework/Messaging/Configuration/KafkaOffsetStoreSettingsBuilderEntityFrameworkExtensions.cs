// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore;
using Silverback.Storage;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Adds the <seealso cref="UseEntityFramework{TDbContext}" /> method to the <see cref="KafkaOffsetStoreSettingsBuilder" />.
/// </summary>
public static class KafkaOffsetStoreSettingsBuilderEntityFrameworkExtensions
{
    /// <summary>
    ///     Configures the Entity Framework based offset store.
    /// </summary>
    /// <typeparam name="TDbContext">
    ///     The type of the <see cref="DbContext" />.
    /// </typeparam>
    /// <param name="builder">
    ///     The <see cref="KafkaOffsetStoreSettingsBuilder" />.
    /// </param>
    /// <returns>
    ///     The <see cref="EntityFrameworkKafkaOffsetStoreSettingsBuilder" />.
    /// </returns>
    [SuppressMessage("Style", "IDE0060:Remove unused parameter", Justification = "Extension method (fluent API)")]
    public static EntityFrameworkKafkaOffsetStoreSettingsBuilder UseEntityFramework<TDbContext>(this KafkaOffsetStoreSettingsBuilder builder)
        where TDbContext : DbContext =>
        new(typeof(TDbContext), SilverbackDbContextFactory.CreateDbContext<TDbContext>);
}
