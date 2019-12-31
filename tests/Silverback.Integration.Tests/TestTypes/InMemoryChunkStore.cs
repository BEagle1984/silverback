// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.LargeMessages;

namespace Silverback.Tests.Integration.TestTypes
{
    public class InMemoryChunkStore : TransactionalList<InMemoryStoredChunk>, IChunkStore
    {
        private string _pendingCleanup;

        public async Task Store(string messageId, int chunkId, int chunksCount, byte[] content) =>
            await Add(new InMemoryStoredChunk
            {
                MessageId = messageId,
                ChunkId = chunkId,
                Content = content
            });

        public Task<int> CountChunks(string messageId) =>
            Task.FromResult(Entries.Union(UncommittedEntries)
                .Where(e => e.MessageId == messageId)
                .Select(e => e.ChunkId)
                .Distinct()
                .Count());

        public Task<Dictionary<int, byte[]>> GetChunks(string messageId) =>
            Task.FromResult(Entries.Union(UncommittedEntries)
                .Where(e => e.MessageId == messageId)
                .GroupBy(e => e.ChunkId)
                .Select(g => g.First())
                .ToDictionary(e => e.ChunkId, e => e.Content));

        public Task Cleanup(string messageId)
        {
            _pendingCleanup = messageId;

            return Task.CompletedTask;
        }

        public override async Task Commit()
        {
            if (!string.IsNullOrEmpty(_pendingCleanup))
            {
                Entries.RemoveAll(e => e.MessageId == _pendingCleanup);
                UncommittedEntries.RemoveAll(e => e.MessageId == _pendingCleanup);
            }

            await base.Commit();
        }

        public override async Task Rollback()
        {
            _pendingCleanup = null;
            await base.Rollback();
        }
    }
}