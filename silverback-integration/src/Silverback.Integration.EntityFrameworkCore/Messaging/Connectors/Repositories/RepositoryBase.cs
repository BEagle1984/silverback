using System;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using Silverback.Messaging.Connectors.Model;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Connectors.Repositories
{
    public abstract class RepositoryBase<TEntity>
        where TEntity : class
    {
        protected readonly DbContext DbContext;
        protected readonly DbSet<TEntity> DbSet;

        protected RepositoryBase(DbContext dbContext)
        {
            DbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            DbSet = dbContext.Set<TEntity>() ?? throw new SilverbackException($"The DbContext doesn't contain a DbSet<{typeof(TEntity).Name}>.");
        }

        protected static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto
        };
    }
}