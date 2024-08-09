// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.EntityFrameworkCore;
using Silverback.Messaging.Consuming.KafkaOffsetStore;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="EntityFrameworkKafkaOffsetStoreSettings" />.
/// </summary>
public class EntityFrameworkKafkaOffsetStoreSettingsBuilder : IKafkaOffsetStoreSettingsImplementationBuilder
{
    private readonly Type _dbContextType;

    private readonly Func<IServiceProvider, ISilverbackContext?, DbContext> _dbContextFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EntityFrameworkKafkaOffsetStoreSettingsBuilder" /> class.
    /// </summary>
    /// <param name="dbContextType">
    ///     The type of the <see cref="DbContext" /> to be used to access the database.
    /// </param>
    /// <param name="dbContextFactory">
    ///     The factory method that creates the <see cref="DbContext" /> instance.
    /// </param>
    public EntityFrameworkKafkaOffsetStoreSettingsBuilder(Type dbContextType, Func<IServiceProvider, ISilverbackContext?, DbContext> dbContextFactory)
    {
        _dbContextType = dbContextType;
        _dbContextFactory = dbContextFactory;
    }

    /// <inheritdoc cref="IKafkaOffsetStoreSettingsImplementationBuilder.Build" />
    public KafkaOffsetStoreSettings Build()
    {
        EntityFrameworkKafkaOffsetStoreSettings settings = new(_dbContextType, _dbContextFactory);

        settings.Validate();

        return settings;
    }
}
