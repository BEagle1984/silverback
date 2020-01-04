﻿// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Silverback.Database;
using Silverback.Infrastructure;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors.Model;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class DbOffsetStore : RepositoryBase<StoredOffset>, IOffsetStore
    {
        private static readonly JsonSerializerSettings SerializerSettings;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        static DbOffsetStore()
        {
            SerializerSettings = new JsonSerializerSettings
            {
                Formatting = Formatting.None,
                DateFormatHandling = DateFormatHandling.IsoDateFormat,
                NullValueHandling = NullValueHandling.Ignore,
                DefaultValueHandling = DefaultValueHandling.Ignore,
                TypeNameHandling = TypeNameHandling.Auto
            };
        }

        public DbOffsetStore(IDbContext dbContext) : base(dbContext)
        {
        }

        public async Task Store(IComparableOffset offset)
        {
            await _semaphore.WaitAsync();

            try
            {
                var entity = await DbSet.FindAsync(offset.Key) ??
                             DbSet.Add(new StoredOffset
                             {
                                 Key = offset.Key
                             });

                entity.Offset = JsonConvert.SerializeObject(offset, typeof(IOffset), SerializerSettings);
            }
            finally
            {
                _semaphore.Release();
            }
        }

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

        public Task Rollback()
        {
            // Nothing to do, just not saving the changes made to the DbContext
            return Task.CompletedTask;
        }

        public async Task<IComparableOffset> GetLatestValue(string key)
        {
            var storedOffset = await DbSet.FindAsync(key);

            return storedOffset?.Offset != null 
                ? JsonConvert.DeserializeObject<IComparableOffset>(storedOffset.Offset, SerializerSettings)
                : null;
        }
    }
}
