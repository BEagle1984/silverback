// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Microsoft.EntityFrameworkCore;
using Silverback.Infrastructure;

namespace Silverback.Messaging.LargeMessages
{
    public class DbContextChunkStore : RepositoryBase<TemporaryMessageChunk>, IChunkStore
    {
        public DbContextChunkStore(DbContext dbContext) : base(dbContext)
        {
        }

        public void StoreChunk(MessageChunk chunk)
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

        public int CountChunks(string messageId) => DbSet.Count(c => c.OriginalMessageId == messageId);

        public Dictionary<int, byte[]> GetChunks(string messageId) =>
            DbSet.Where(c => c.OriginalMessageId == messageId).ToDictionary(c => c.ChunkId, c => c.Content);

        public void Cleanup(string messageId)
        {
            var entities = DbSet.Local.Where(c => c.OriginalMessageId == messageId).ToList();

            if (!entities.Any() || entities.Count != entities.First().ChunksCount)
            {
                entities = DbSet.Where(c => c.OriginalMessageId == messageId)
                    .Select(c => c.ChunkId).ToList()
                    .Select(x => new TemporaryMessageChunk {OriginalMessageId = messageId, ChunkId = x}).ToList();
            }

            DbSet.RemoveRange(entities);
        }
    }
}
