using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Silverback.Infrastructure;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors.Model;

namespace Silverback.Messaging.Connectors.Repositories
{
    public class DbContextOffsetStore : RepositoryBase<StoredOffset>, IOffsetStore
    {
        private static readonly JsonSerializerSettings SerializerSettings;
        private readonly object _lock = new object();

        static DbContextOffsetStore()
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

        public DbContextOffsetStore(DbContext dbContext) : base(dbContext)
        {
        }

        public void Store(IOffset offset)
        {
            lock (_lock)
            {
                var entity = DbSet.Find(offset.Key) ??
                             DbSet.Add(new StoredOffset
                             {
                                 Key = offset.Key
                             }).Entity;

                entity.Offset = JsonConvert.SerializeObject(offset, typeof(IOffset), SerializerSettings);
            }
        }

        public void Commit()
        {
            lock (_lock)
            {
                // Call SaveChanges, in case it isn't called by a subscriber
                DbContext.SaveChanges();
            }
        }

        public void Rollback()
        {
            // Nothing to do, just not saving the changes made to the DbContext
        }

        public IOffset GetLatestValue(string key)
        {
            var json = DbSet
                .Where(o => o.Key == key)
                .Select(o => o.Offset)
                .FirstOrDefault();

            return json != null 
                ? JsonConvert.DeserializeObject<IOffset>(json, SerializerSettings)
                : null;
        }
    }
}
