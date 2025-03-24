// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.EntityFrameworkCore;

namespace Silverback.Storage;

/// <summary>
///     The basic settings of the Entity Framework based implementations.
/// </summary>
public interface IEntityFrameworkSettings
{
    /// <summary>
    ///     Gets the type of the <see cref="DbContext" /> to be used to access the database.
    /// </summary>
    Type DbContextType { get; }

    /// <summary>
    ///     Gets the factory method that creates the <see cref="DbContext" /> instance.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the <see cref="DbContext" />.
    /// </param>
    /// <param name="context">
    ///     The <see cref="ISilverbackContext" />.
    /// </param>
    /// <returns>
    ///     The <see cref="DbContext" /> instance.
    /// </returns>
    DbContext GetDbContext(IServiceProvider serviceProvider, ISilverbackContext? context = null);
}
