// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.LargeMessages;

namespace Silverback.Tests.Integration.TestTypes
{
    public class InMemoryChunkStore : TransactionalList<InMemoryStoredChunk>, IChunkStore
    {
        public void Store(MessageChunk chunk) =>
            Add(new InMemoryStoredChunk
            {
                MessageId = chunk.OriginalMessageId,
                ChunkId = chunk.ChunkId,
                Content = chunk.Content
            });

        public int CountChunks(string messageId) =>
            Entries.Union(UncommittedEntries)
                .Where(e => e.MessageId == messageId)
                .Select(e => e.ChunkId)
                .Distinct()
                .Count();

        public Dictionary<int, byte[]> GetChunks(string messageId) =>
            Entries.Union(UncommittedEntries)
                .Where(e => e.MessageId == messageId)
                .GroupBy(e => e.ChunkId)
                .Select(g => g.First())
                .ToDictionary(e => e.ChunkId, e => e.Content);

        public void Cleanup(string messageId)
        {
            Entries.RemoveAll(e => e.MessageId == messageId);
            UncommittedEntries.RemoveAll(e => e.MessageId == messageId);
        }
    }
}
