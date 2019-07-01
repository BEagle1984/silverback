// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Silverback.Infrastructure;

namespace Silverback.Messaging.LargeMessages
{
    public class DbContextChunkStore : RepositoryBase<TemporaryMessageChunk>, IChunkStore
    {
        private readonly object _lock = new object();

        public DbContextChunkStore(DbContext dbContext) : base(dbContext)
        {
        }

        public void Store(string messageId, int chunkId, int chunksCount, byte[] content)
        {
            lock (_lock)
            {
                // TODO: Log?
                if (DbSet.Any(c => c.OriginalMessageId == messageId && c.ChunkId == chunkId))
                    return;

                DbSet.Add(new TemporaryMessageChunk
                {
                    OriginalMessageId = messageId,
                    ChunkId = chunkId,
                    ChunksCount = chunksCount,
                    Content = content,
                    Received = DateTime.UtcNow
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
            lock (_lock)
            {
                var entities = DbSet.Local.Where(c => c.OriginalMessageId == messageId).ToList();

                // Chunks are always loaded all together for the message, therefore if any
                // is in cache it means that we got all of them.
                if (!entities.Any())
                {
                    entities = DbSet
                        .Where(c => c.OriginalMessageId == messageId)
                        .Select(c => c.ChunkId)
                        .ToList()
                        .Select(chunkId => new TemporaryMessageChunk {OriginalMessageId = messageId, ChunkId = chunkId})
                        .ToList();
                }

                DbSet.RemoveRange(entities);
            }
        }
    }
}
