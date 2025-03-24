// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Storage;
using Silverback.Util;

namespace Silverback.Messaging.Consuming.KafkaOffsetStore;

/// <summary>
///     Stores the latest consumed offsets in EntityFramework.
/// </summary>
public class EntityFrameworkKafkaOffsetStore : IKafkaOffsetStore
{
    private readonly EntityFrameworkKafkaOffsetStoreSettings _settings;

    private readonly IServiceScopeFactory _serviceScopeFactory;

    /// <summary>
    ///     Initializes a new instance of the <see cref="EntityFrameworkKafkaOffsetStore" /> class.
    /// </summary>
    /// <param name="settings">
    ///     The offset store settings.
    /// </param>
    /// <param name="serviceScopeFactory">
    ///     The <see cref="IServiceScopeFactory" />.
    /// </param>
    public EntityFrameworkKafkaOffsetStore(EntityFrameworkKafkaOffsetStoreSettings settings, IServiceScopeFactory serviceScopeFactory)
    {
        _settings = Check.NotNull(settings, nameof(settings));
        _serviceScopeFactory = Check.NotNull(serviceScopeFactory, nameof(serviceScopeFactory));
    }

    /// <inheritdoc cref="IKafkaOffsetStore.GetStoredOffsets" />
    public IReadOnlyCollection<KafkaOffset> GetStoredOffsets(string groupId)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        using DbContext dbContext = _settings.GetDbContext(scope.ServiceProvider);

        IQueryable<KafkaOffset> offsets = dbContext.Set<SilverbackStoredOffset>()
            .AsNoTracking()
            .Where(storedOffset => storedOffset.GroupId == groupId)
            .Select(storedOffset => new KafkaOffset(storedOffset.Topic, storedOffset.Partition, storedOffset.Offset));

        return [.. offsets];
    }

    /// <inheritdoc cref="IKafkaOffsetStore.StoreOffsetsAsync" />
    public async Task StoreOffsetsAsync(string groupId, IEnumerable<KafkaOffset> offsets, ISilverbackContext? context = null)
    {
        using IServiceScope scope = _serviceScopeFactory.CreateScope();
        using DbContext dbContext = _settings.GetDbContext(scope.ServiceProvider, context);

        dbContext.UseTransactionIfAvailable(context);

        List<SilverbackStoredOffset> storedOffsets = await dbContext.Set<SilverbackStoredOffset>()
            .AsTracking()
            .Where(storedOffset => storedOffset.GroupId == groupId)
            .ToListAsync()
            .ConfigureAwait(false);

        foreach (KafkaOffset offset in Check.NotNull(offsets, nameof(offsets)))
        {
            SilverbackStoredOffset? storedOffset = storedOffsets.Find(
                storedOffset => storedOffset.Topic == offset.TopicPartition.Topic &&
                                storedOffset.Partition == offset.TopicPartition.Partition.Value);

            if (storedOffset == null)
            {
                storedOffset = new SilverbackStoredOffset
                {
                    GroupId = groupId,
                    Topic = offset.TopicPartition.Topic,
                    Partition = offset.TopicPartition.Partition.Value
                };

                dbContext.Set<SilverbackStoredOffset>().Add(storedOffset);
                storedOffsets.Add(storedOffset);
            }

            storedOffset.Offset = offset.Offset.Value;
        }

        await dbContext.SaveChangesAsync().ConfigureAwait(false);
    }
}
