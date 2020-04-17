// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Util;

namespace Silverback.Messaging.LargeMessages
{
    /// <summary>
    ///     Temporary stores the message chunks in memory, waiting for the full message to be available.
    /// </summary>
    /// <inheritdoc cref="IChunkStore" />
    public class InMemoryChunkStore : TransactionalList<InMemoryStoredChunk>, IChunkStore
    {
        private readonly List<string> _pendingCleanups = new List<string>();

        public InMemoryChunkStore(TransactionalListSharedItems<InMemoryStoredChunk> sharedItems)
            : base(sharedItems)
        {
        }

        public bool HasNotPersistedChunks =>
            Items.Any(item => !_pendingCleanups.Contains(item.Entry.MessageId)) ||
            UncommittedItems.Any(item => !_pendingCleanups.Contains(item.Entry.MessageId));

        public async Task Store(string messageId, int chunkId, int chunksCount, byte[] content) =>
            await Add(new InMemoryStoredChunk
            {
                MessageId = messageId,
                ChunkId = chunkId,
                Content = content
            });

        public Task<int> CountChunks(string messageId) =>
            Task.FromResult(Items.Union(UncommittedItems)
                .Where(item => item.Entry.MessageId == messageId)
                .Select(item => item.Entry.ChunkId)
                .Distinct()
                .Count());

        public Task<Dictionary<int, byte[]>> GetChunks(string messageId) =>
            Task.FromResult(Items.Union(UncommittedItems)
                .Where(item => item.Entry.MessageId == messageId)
                .GroupBy(item => item.Entry.ChunkId)
                .Select(items => items.First())
                .ToDictionary(item => item.Entry.ChunkId, item => item.Entry.Content));

        public Task Cleanup(string messageId)
        {
            _pendingCleanups.Add(messageId);

            return Task.CompletedTask;
        }

        public Task Cleanup(DateTime threshold)
        {
            lock (Items)
            {
                Items.RemoveAll(item => item.InsertDate < threshold);
            }

            return Task.CompletedTask;
        }

        public override async Task Commit()
        {
            if (_pendingCleanups.Any())
            {
                lock (UncommittedItems)
                {
                    UncommittedItems.RemoveAll(item => _pendingCleanups.Contains(item.Entry.MessageId));
                }

                lock (Items)
                {
                    Items.RemoveAll(item => _pendingCleanups.Contains(item.Entry.MessageId));
                }

                _pendingCleanups.Clear();
            }

            await base.Commit();
        }

        public override async Task Rollback()
        {
            _pendingCleanups.Clear();
            await base.Rollback();
        }
    }
}