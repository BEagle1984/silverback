﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Tests.Integration.TestTypes
{
    public class InMemoryStoredChunk
    {
        public string MessageId { get; set; }

        public int ChunkId { get; set; }

        public byte[] Content { get; set; }
    }
}