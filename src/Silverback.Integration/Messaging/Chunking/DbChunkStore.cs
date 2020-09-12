// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Database;
using Silverback.Database.Model;
using Silverback.Infrastructure;
using Silverback.Messaging.Inbound.Transaction;

namespace Silverback.Messaging.Chunking
{
    /// <summary>
    ///     Stores the message chunks into the database, waiting for the full message to be available.
    /// </summary>
    public sealed class DbChunkStore : RepositoryBase<TemporaryMessageChunk>, IChunkStore, IDisposable
    {
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        /// <summary>
        ///     Initializes a new instance of the <see cref="DbChunkStore" /> class.
        /// </summary>
        /// <param name="dbContext">
        ///     The <see cref="IDbContext" /> to use as storage.
        /// </param>
        public DbChunkStore(IDbContext dbContext)
            : base(dbContext)
        {
        }

        /// <inheritdoc cref="IChunkStore.HasNotPersistedChunks" />
        [SuppressMessage(
            "ReSharper",
            "UnassignedGetOnlyAutoProperty",
            Justification = "Unused in this implementation, but declared in interface.")]
        public bool HasNotPersistedChunks { get; }

        /// <inheritdoc cref="IChunkStore.Store" />
        public async Task Store(string messageId, int chunkIndex, int chunksCount, byte[] content)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                // TODO: Log?
                if (await DbSet.AsQueryable().AnyAsync(
                        chunk => chunk.MessageId == messageId && chunk.ChunkIndex == chunkIndex)
                    .ConfigureAwait(false))
                {
                    return;
                }

                DbSet.Add(
                    new TemporaryMessageChunk
                    {
                        MessageId = messageId,
                        ChunkIndex = chunkIndex,
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

        /// <inheritdoc cref="ITransactional.Commit" />
        public async Task Commit()
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                // Call SaveChanges, in case it isn't called by a subscriber
                await DbContext.SaveChangesAsync().ConfigureAwait(false);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc cref="ITransactional.Rollback" />
        public Task Rollback()
        {
            // Nothing to do, just not saving the changes made to the DbContext
            return Task.CompletedTask;
        }

        /// <inheritdoc cref="IChunkStore.CountChunks" />
        public Task<int> CountChunks(string messageId) =>
            DbSet.AsQueryable().CountAsync(chunk => chunk.MessageId == messageId);

        /// <inheritdoc cref="IChunkStore.GetChunks" />
        public Task<Dictionary<int, byte[]>> GetChunks(string messageId) =>
            DbSet.AsQueryable()
                .Where(chunk => chunk.MessageId == messageId)
                .ToDictionaryAsync(chunk => chunk.ChunkIndex, chunk => chunk.Content);

        /// <inheritdoc cref="IChunkStore.Cleanup(string)" />
        public async Task Cleanup(string messageId)
        {
            await _semaphore.WaitAsync().ConfigureAwait(false);

            try
            {
                var entities = DbSet.GetLocalCache().Where(c => c.MessageId == messageId).ToList();

                // Chunks are always loaded all together for the message, therefore if any
                // is in cache it means that we got all of them.
                if (!entities.Any())
                {
                    entities = (await DbSet
                            .AsQueryable()
                            .Where(chunk => chunk.MessageId == messageId)
                            .Select(chunk => chunk.ChunkIndex)
                            .ToListAsync()
                            .ConfigureAwait(false))
                        .Select(
                            chunkId => new TemporaryMessageChunk
                                { MessageId = messageId, ChunkIndex = chunkId })
                        .ToList();
                }

                DbSet.RemoveRange(entities);
            }
            finally
            {
                _semaphore.Release();
            }
        }

        /// <inheritdoc cref="IChunkStore.Cleanup(System.DateTime)" />
        public async Task Cleanup(DateTime threshold)
        {
            var expiredEntities = (await DbSet
                    .AsQueryable()
                    .Where(chunk => chunk.Received < threshold)
                    .Select(chunk => new { chunk.ChunkIndex, OriginalMessageId = chunk.MessageId })
                    .ToListAsync()
                    .ConfigureAwait(false))
                .Select(
                    chunk => new TemporaryMessageChunk
                        { MessageId = chunk.OriginalMessageId, ChunkIndex = chunk.ChunkIndex })
                .ToList();

            DbSet.RemoveRange(expiredEntities);

            await DbContext.SaveChangesAsync().ConfigureAwait(false);
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            _semaphore.Dispose();
        }
    }
}
