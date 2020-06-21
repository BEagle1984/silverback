// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Silverback.Database;
using Silverback.Database.Model;
using Silverback.Infrastructure;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Connectors.Repositories
{
    /// <summary>
    ///     <para>
    ///         Used by the <see cref="OffsetStoredInboundConnector" /> to keep track of the last processed
    ///         offsets and guarantee that each message is processed only once.
    ///     </para>
    ///     <para>
    ///         An <see cref="IDbContext" /> is used to store the offsets into the database.
    ///     </para>
    /// </summary>
    // TODO: Test maybe?
    public sealed class DbOffsetStore : RepositoryBase<StoredOffset>, IOffsetStore
    {
        private static readonly JsonSerializerSettings SerializerSettings =
            new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto
            };

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

        /// <inheritdoc cref="IOffsetStore.Store" />
        public async Task Store(IComparableOffset offset, IConsumerEndpoint endpoint)
        {
            Check.NotNull(offset, nameof(offset));
            Check.NotNull(endpoint, nameof(endpoint));

            var storedOffsetEntity = await DbSet.FindAsync(GetKey(offset.Key, endpoint)) ??
                                     DbSet.Add(
                                         new StoredOffset
                                         {
                                             Key = GetKey(offset.Key, endpoint),
                                             ClrType = offset.GetType().AssemblyQualifiedName
                                         });

            storedOffsetEntity.Value = offset.Value;

#pragma warning disable 618
            storedOffsetEntity.Offset = null;
#pragma warning restore 618
        }

        /// <inheritdoc cref="ITransactional.Commit" />
        public async Task Commit()
        {
            // Call SaveChanges, in case it isn't called by a subscriber
            await DbContext.SaveChangesAsync();
        }

        /// <inheritdoc cref="ITransactional.Rollback" />
        public Task Rollback()
        {
            // Nothing to do, just not saving the changes made to the DbContext
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="IOffsetStore.GetLatestValue" />
        public async Task<IComparableOffset?> GetLatestValue(string offsetKey, IConsumerEndpoint endpoint)
        {
            Check.NotNull(offsetKey, nameof(offsetKey));
            Check.NotNull(endpoint, nameof(endpoint));

            var storedOffsetEntity = await DbSet.FindAsync(GetKey(offsetKey, endpoint)) ??
                                     await DbSet.FindAsync(offsetKey);

            return DeserializeOffset(storedOffsetEntity);
        }

        private static IComparableOffset? DeserializeOffset(StoredOffset? storedOffsetEntity)
        {
            if (storedOffsetEntity == null)
                return null;

            if (storedOffsetEntity.Value != null)
            {
                var offsetType = TypesCache.GetType(storedOffsetEntity.ClrType!);
                var offset = (IComparableOffset)Activator.CreateInstance(
                    offsetType,
                    storedOffsetEntity.Key,
                    storedOffsetEntity.Value);

                return offset;
            }

#pragma warning disable 618
            if (storedOffsetEntity.Offset != null)
            {
                return JsonConvert.DeserializeObject<IComparableOffset>(storedOffsetEntity.Offset, SerializerSettings);
            }
#pragma warning restore 618

            throw new InvalidOperationException("The offset cannot be deserialized. Both Value and Offset are null.");
        }

        private static string GetKey(string offsetKey, IConsumerEndpoint endpoint) =>
            $"{endpoint.GetUniqueConsumerGroupName()}|{offsetKey}";
    }
}
