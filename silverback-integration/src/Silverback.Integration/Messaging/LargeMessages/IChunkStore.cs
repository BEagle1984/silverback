// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;

namespace Silverback.Messaging.LargeMessages
{
    public interface IChunkStore
    {
        void StoreChunk(string messageId, int chunkId, byte[] content);

        int CountChunks(string messageId);

        Dictionary<int, byte[]> GetChunks(string messageId);

        void Cleanup(string messageId);
    }
}