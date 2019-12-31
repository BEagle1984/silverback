// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Silverback.Messaging.LargeMessages
{
    public interface IChunkStore
    {
        Task Store(string messageId, int chunkId, int chunksCount, byte[] content);

        Task Commit();

        Task Rollback();

        Task<int> CountChunks(string messageId);

        Task<Dictionary<int, byte[]>> GetChunks(string messageId);

        Task Cleanup(string messageId);
    }
}