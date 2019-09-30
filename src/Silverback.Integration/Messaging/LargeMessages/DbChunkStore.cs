// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Database;
using Silverback.Infrastructure;

namespace Silverback.Messaging.LargeMessages
{
    public class DbChunkStore : RepositoryBase<TemporaryMessageChunk>, IChunkStore
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public DbChunkStore(IDbContext dbContext) : base(dbContext)
        {
        }

        public async Task Store(string messageId, int chunkId, int chunksCount, byte[] content)
        {
            await _semaphore.WaitAsync();

            try
            {
                // TODO: Log?
                if (await DbSet.AsQueryable().AnyAsync(c => c.OriginalMessageId == messageId && c.ChunkId == chunkId))
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

        public Task<int> CountChunks(string messageId) => DbSet.AsQueryable().CountAsync(c => c.OriginalMessageId == messageId);

        public Task<Dictionary<int, byte[]>> GetChunks(string messageId) =>
            DbSet.AsQueryable()
                .Where(c => c.OriginalMessageId == messageId)
                .ToDictionaryAsync(c => c.ChunkId, c => c.Content);

        public async Task Cleanup(string messageId)
        {
            await _semaphore.WaitAsync();

            try
            {
                var entities = DbSet.GetLocalCache().Where(c => c.OriginalMessageId == messageId).ToList();

                // Chunks are always loaded all together for the message, therefore if any
                // is in cache it means that we got all of them.
                if (!entities.Any())
                {
                    entities = (await DbSet
                            .AsQueryable()
                            .Where(c => c.OriginalMessageId == messageId)
                            .Select(c => c.ChunkId)
                            .ToListAsync())
                        .Select(chunkId => new TemporaryMessageChunk
                            {OriginalMessageId = messageId, ChunkId = chunkId})
                        .ToList();
                }

                DbSet.RemoveRange(entities);
            }
            finally
            {
                _semaphore.Release();
            }
        }
    }
}
