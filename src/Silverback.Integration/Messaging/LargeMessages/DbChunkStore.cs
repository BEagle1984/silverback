// Copyright (c) 2020 Sergio Aquilini
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
    /// <summary>
    ///     Stores the message chunks into the database, waiting for the full message to be available.
    /// </summary>
    /// <inheritdoc cref="IChunkStore" />
    public class DbChunkStore : RepositoryBase<TemporaryMessageChunk>, IChunkStore
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        public DbChunkStore(IDbContext dbContext)
            : base(dbContext)
        {
        }

        public bool HasNotPersistedChunks { get; } = false;

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

        public Task<int> CountChunks(string messageId) =>
            DbSet.AsQueryable().CountAsync(chunk => chunk.OriginalMessageId == messageId);

        public Task<Dictionary<int, byte[]>> GetChunks(string messageId) =>
            DbSet.AsQueryable()
                .Where(chunk => chunk.OriginalMessageId == messageId)
                .ToDictionaryAsync(chunk => chunk.ChunkId, chunk => chunk.Content);

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
                            .Where(chunk => chunk.OriginalMessageId == messageId)
                            .Select(chunk => chunk.ChunkId)
                            .ToListAsync())
                        .Select(chunkId => new TemporaryMessageChunk
                            { OriginalMessageId = messageId, ChunkId = chunkId })
                        .ToList();
                }

                DbSet.RemoveRange(entities);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public async Task Cleanup(DateTime threshold)
        {
            var expiredEntities = (await DbSet
                    .AsQueryable()
                    .Where(chunk => chunk.Received < threshold)
                    .Select(chunk => new { chunk.ChunkId, chunk.OriginalMessageId })
                    .ToListAsync())
                .Select(chunk => new TemporaryMessageChunk
                    { OriginalMessageId = chunk.OriginalMessageId, ChunkId = chunk.ChunkId })
                .ToList();

            DbSet.RemoveRange(expiredEntities);

            await DbContext.SaveChangesAsync();
        }
    }
}