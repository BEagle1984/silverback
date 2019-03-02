// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)
namespace Silverback.Tests.TestTypes
{
    public class InMemoryStoredChunk
    {
        public string MessageId { get; set; }

        public int ChunkId { get; set; }

        public byte[] Content { get; set; }
    }
}