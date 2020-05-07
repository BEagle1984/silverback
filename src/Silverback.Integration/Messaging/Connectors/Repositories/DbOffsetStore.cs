// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Silverback.Database;
using Silverback.Database.Model;
using Silverback.Infrastructure;
using Silverback.Messaging.Broker;

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
    public sealed class DbOffsetStore : RepositoryBase<StoredOffset>, IOffsetStore, IDisposable
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

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

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

        /// <inheritdoc />
        public async Task Store(IComparableOffset offset, IConsumerEndpoint endpoint)
        {
            if (offset == null)
                throw new ArgumentNullException(nameof(offset));

            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));

            await _semaphore.WaitAsync();

            try
            {
                var storedOffsetEntity = await DbSet.FindAsync(GetKey(offset.Key, endpoint)) ??
                                         DbSet.Add(
                                             new StoredOffset
                                             {
                                                 Key = GetKey(offset.Key, endpoint)
                                             });

                storedOffsetEntity.Offset = JsonConvert.SerializeObject(offset, typeof(IOffset), SerializerSettings);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc />
        public async Task Commit()
        {
            await _semaphore.WaitAsync();

            try
            {
                // Call SaveChanges, in case it isn't called by a subscriber
                await DbContext.SaveChangesAsync();
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc />
        public Task Rollback()
        {
            // Nothing to do, just not saving the changes made to the DbContext
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public async Task<IComparableOffset?> GetLatestValue(string offsetKey, IConsumerEndpoint endpoint)
        {
            if (offsetKey == null)
                throw new ArgumentNullException(nameof(offsetKey));

            if (endpoint == null)
                throw new ArgumentNullException(nameof(endpoint));

            var storedOffsetEntity = await DbSet.FindAsync(GetKey(offsetKey, endpoint)) ??
                                     await DbSet.FindAsync(offsetKey);

            return storedOffsetEntity?.Offset != null
                ? JsonConvert.DeserializeObject<IComparableOffset>(storedOffsetEntity.Offset, SerializerSettings)
                : null;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _semaphore.Dispose();
        }

        private string GetKey(string offsetKey, IConsumerEndpoint endpoint) =>
            $"{endpoint.GetUniqueConsumerGroupName()}|{offsetKey}";
    }
}
