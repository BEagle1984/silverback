// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Silverback.Database;
using Silverback.Database.Model;
using Silverback.Infrastructure;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Inbound.Transaction;
using Silverback.Util;

namespace Silverback.Messaging.Inbound.ExactlyOnce.Repositories;

/// <summary>
///     <para>
///         Used by the <see cref="OffsetStoreExactlyOnceStrategy" /> to keep track of the last processed
///         offsets and guarantee that each message is processed only once.
///     </para>
///     <para>
///         An <see cref="IDbContext" /> is used to store the offsets into the database.
///     </para>
/// </summary>
public sealed class DbOffsetStore : RepositoryBase<StoredOffset>, IOffsetStore
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DbOffsetStore" /> class.
    /// </summary>
    /// <param name="dbContext">
    ///     The <see cref="IDbContext" /> to use as storage.
    /// </param>
    public DbOffsetStore(IDbContext dbContext)
        : base(dbContext)
    {
    }

    /// <inheritdoc cref="IOffsetStore.StoreAsync" />
    public async Task StoreAsync(IBrokerMessageOffset offset, ConsumerConfiguration consumerConfiguration)
    {
        Check.NotNull(offset, nameof(offset));
        Check.NotNull(consumerConfiguration, nameof(consumerConfiguration));

        StoredOffset storedOffsetEntity = await DbSet.FindAsync(GetKey(offset.Key, consumerConfiguration)).ConfigureAwait(false) ??
                                          DbSet.Add(
                                              new StoredOffset
                                              {
                                                  Key = GetKey(offset.Key, consumerConfiguration),
                                                  ClrType = offset.GetType().AssemblyQualifiedName
                                              });

        storedOffsetEntity.Value = offset.Value;

#pragma warning disable CS0618 // Obsolete
        storedOffsetEntity.Offset = null;
#pragma warning restore CS0618 // Obsolete
    }

    /// <inheritdoc cref="ITransactional.CommitAsync" />
    public async Task CommitAsync()
    {
        // Call SaveChanges, in case it isn't called by a subscriber
        await DbContext.SaveChangesAsync().ConfigureAwait(false);
    }

    /// <inheritdoc cref="ITransactional.RollbackAsync" />
    public Task RollbackAsync()
    {
        // Nothing to do, just not saving the changes made to the DbContext
        return Task.CompletedTask;
    }

    /// <inheritdoc cref="IOffsetStore.GetLatestValueAsync" />
    public async Task<IBrokerMessageOffset?> GetLatestValueAsync(string offsetKey, ConsumerConfiguration consumerConfiguration)
    {
        Check.NotNull(offsetKey, nameof(offsetKey));
        Check.NotNull(consumerConfiguration, nameof(consumerConfiguration));

        StoredOffset? storedOffsetEntity = await DbSet.FindAsync(GetKey(offsetKey, consumerConfiguration)).ConfigureAwait(false)
                                           ?? await DbSet.FindAsync(offsetKey).ConfigureAwait(false);

        return DeserializeOffset(storedOffsetEntity);
    }

    private static IBrokerMessageOffset? DeserializeOffset(StoredOffset? storedOffsetEntity)
    {
        if (storedOffsetEntity == null)
            return null;

        if (storedOffsetEntity.Value != null && storedOffsetEntity.ClrType != null)
        {
            return InstantiateOffset(storedOffsetEntity.ClrType, storedOffsetEntity.Key, storedOffsetEntity.Value);
        }

#pragma warning disable CS0618 // Obsolete
        if (storedOffsetEntity.Offset != null)
        {
            LegacyOffsetModel? legacyOffset = JsonSerializer.Deserialize<LegacyOffsetModel>(storedOffsetEntity.Offset);

            if (legacyOffset?.TypeName == null || legacyOffset.Value == null)
                throw new InvalidOperationException("Failed to deserialize legacy offset.");

            return InstantiateOffset(legacyOffset.TypeName, storedOffsetEntity.Key, legacyOffset.Value);
        }
#pragma warning restore CS0618 // Obsolete

        throw new InvalidOperationException("The offset cannot be deserialized. Both ClrType/Value and Offset are null.");
    }

    private static IBrokerMessageOffset InstantiateOffset(string clrType, string key, string value)
    {
        Type offsetType = TypesCache.GetType(clrType);
        IBrokerMessageOffset offset = (IBrokerMessageOffset)Activator.CreateInstance(offsetType, key, value)!;
        return offset;
    }

    private static string GetKey(string offsetKey, ConsumerConfiguration consumerConfiguration) =>
        $"{consumerConfiguration.GetUniqueConsumerGroupName()}|{offsetKey}";

    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Local", Justification = "Used in Deserialize method")]
    private sealed class LegacyOffsetModel
    {
        [JsonPropertyName("$type")]
        public string? TypeName { get; set; }

        public string? Value { get; set; }
    }
}
