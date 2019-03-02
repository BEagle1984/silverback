// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Silverback.Infrastructure;

namespace Silverback.Messaging.LargeMessages
{
    public class DbContextChunkStore : RepositoryBase<TemporaryMessageChunk>, IChunkStore
    {
        private readonly ILogger _logger;
        private readonly object _lock = new object();

        public DbContextChunkStore(DbContext dbContext, ILogger<DbContextChunkStore> logger) : base(dbContext)
        {
            _logger = logger;
        }

        public void Store(MessageChunk chunk)
        {
            lock (_lock)
            {
                // TODO: Log?
                if (DbSet.Any(c => c.OriginalMessageId == chunk.OriginalMessageId && c.ChunkId == chunk.ChunkId))
                    return;

                DbSet.Add(new TemporaryMessageChunk
                {
                    OriginalMessageId = chunk.OriginalMessageId,
                    ChunkId = chunk.ChunkId,
                    ChunksCount = chunk.ChunksCount,
                    Content = chunk.Content
                });
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

        public int CountChunks(string messageId) => DbSet.Count(c => c.OriginalMessageId == messageId);

        public Dictionary<int, byte[]> GetChunks(string messageId) =>
            DbSet.Where(c => c.OriginalMessageId == messageId).ToDictionary(c => c.ChunkId, c => c.Content);

        public void Cleanup(string messageId)
        {
            try
            {
                lock (_lock)
                {
                    var entities = DbSet.Local.Where(c => c.OriginalMessageId == messageId).ToList();

                    if (!entities.Any() || entities.Count != entities.First().ChunksCount)
                    {
                        entities.AddRange(
                            DbSet
                                .Where(c => c.OriginalMessageId == messageId)
                                .Select(c => c.ChunkId)
                                .ToList()
                                .Where(c => entities.All(e => e.ChunkId != c))
                                .Select(x => new TemporaryMessageChunk {OriginalMessageId = messageId, ChunkId = x}));
                    }

                    DbSet.RemoveRange(entities);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to cleanup chunks from temporary table for message '{messageId}'.", messageId);
            }
        }
    }
}
