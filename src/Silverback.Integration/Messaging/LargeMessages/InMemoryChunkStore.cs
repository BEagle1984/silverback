// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.LargeMessages.Model;
using Silverback.Util;

namespace Silverback.Messaging.LargeMessages
{
    /// <summary>
    ///     Temporary stores the message chunks in memory, waiting for the full message to be available.
    /// </summary>
    public class InMemoryChunkStore : TransactionalList<InMemoryTemporaryMessageChunk>, IChunkStore
    {
        private readonly List<string> _pendingCleanups = new List<string>();

        /// <summary>
        ///     Initializes a new instance of the <see cref="InMemoryChunkStore" /> class.
        /// </summary>
        /// <param name="sharedItems">
        ///     The chunks shared between the instances of this repository.
        /// </param>
        public InMemoryChunkStore(TransactionalListSharedItems<InMemoryTemporaryMessageChunk> sharedItems)
            : base(sharedItems)
        {
        }

        /// <inheritdoc />
        public bool HasNotPersistedChunks =>
            Items.Any(item => !_pendingCleanups.Contains(item.Item.MessageId)) ||
            UncommittedItems.Any(item => !_pendingCleanups.Contains(item.Item.MessageId));

        /// <inheritdoc />
        public async Task Store(string messageId, int chunkIndex, int chunksCount, byte[] content) =>
            await Add(new InMemoryTemporaryMessageChunk(messageId, chunkIndex, content));

        /// <inheritdoc />
        public Task<int> CountChunks(string messageId) =>
            Task.FromResult(
                Items.Union(UncommittedItems)
                    .Where(item => item.Item.MessageId == messageId)
                    .Select(item => item.Item.ChunkIndex)
                    .Distinct()
                    .Count());

        /// <inheritdoc />
        public Task<Dictionary<int, byte[]>> GetChunks(string messageId) =>
            Task.FromResult(
                Items.Union(UncommittedItems)
                    .Where(item => item.Item.MessageId == messageId)
                    .GroupBy(item => item.Item.ChunkIndex)
                    .Select(items => items.First())
                    .ToDictionary(item => item.Item.ChunkIndex, item => item.Item.Content));

        /// <inheritdoc />
        public Task Cleanup(string messageId)
        {
            _pendingCleanups.Add(messageId);

            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task Cleanup(DateTime threshold)
        {
            lock (Items)
            {
                Items.RemoveAll(item => item.InsertDate < threshold);
            }

            return Task.CompletedTask;
        }

        /// <inheritdoc cref="ITransactional.Commit" />
        public override async Task Commit()
        {
            if (_pendingCleanups.Any())
            {
                lock (UncommittedItems)
                {
                    UncommittedItems.RemoveAll(item => _pendingCleanups.Contains(item.Item.MessageId));
                }

                lock (Items)
                {
                    Items.RemoveAll(item => _pendingCleanups.Contains(item.Item.MessageId));
                }

                _pendingCleanups.Clear();
            }

            await base.Commit();
        }

        /// <inheritdoc cref="ITransactional.Rollback" />
        public override async Task Rollback()
        {
            _pendingCleanups.Clear();
            await base.Rollback();
        }
    }
}
