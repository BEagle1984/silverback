// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Xunit;

namespace Silverback.Tests.Integration.E2E.Kafka
{
    public class ChunkingTests
    {
        [Fact]
        public async Task Chunking_Json_ProducedAndConsumed()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_BinaryFile_ProducedAndConsumed()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_Json_AtomicallyCommitted()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_BinaryFile_AtomicallyCommitted()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_Json_BackpressureHandled()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_BinaryFile_BackpressureHandled()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_MultipleSequencesFromMultiplePartitions_ConcurrentlyConsumed()
        {
            throw new NotImplementedException();
        }

        [Fact]
        public async Task Chunking_InterleavedChunks_ExceptionThrown()
        {
            throw new NotImplementedException();
        }

        // TODO: Test with concurrent consumers
    }
}
