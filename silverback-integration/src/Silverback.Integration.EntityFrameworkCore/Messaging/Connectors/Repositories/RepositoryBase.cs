// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;

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

        private static readonly JsonSerializerSettings SerializerSettings = new JsonSerializerSettings
        {
            Formatting = Formatting.None,
            DateFormatHandling = DateFormatHandling.IsoDateFormat,
            NullValueHandling = NullValueHandling.Ignore,
            DefaultValueHandling = DefaultValueHandling.Ignore,
            TypeNameHandling = TypeNameHandling.Auto
        };
        
        protected string Serialize<T>(T obj) => JsonConvert.SerializeObject(obj, typeof(T), SerializerSettings);
        protected T Deserialize<T>(string json) => JsonConvert.DeserializeObject<T>(json, SerializerSettings);
    }
}