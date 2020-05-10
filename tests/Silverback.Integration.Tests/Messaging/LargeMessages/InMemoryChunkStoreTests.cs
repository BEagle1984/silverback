// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.LargeMessages;
using Silverback.Messaging.LargeMessages.Model;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.LargeMessages
{
    public class InMemoryChunkStoreTests
    {
        [Fact]
        public async Task Store_Chunk_ChunkStored()
        {
            var store = new InMemoryChunkStore(new TransactionalListSharedItems<InMemoryTemporaryMessageChunk>());

            await store.Store("123", 0, 10, new byte[10]);
            await store.Store("123", 1, 10, new byte[10]);
            await store.Store("123", 2, 10, new byte[10]);
            await store.Commit();

            store.CommittedItemsCount.Should().Be(3);
        }

        [Fact]
        public async Task CountChunks_WithChunksFromMultipleMessages_CorrectCountReturned()
        {
            var store = new InMemoryChunkStore(new TransactionalListSharedItems<InMemoryTemporaryMessageChunk>());

            await store.Store("123", 0, 10, new byte[10]);
            await store.Store("123", 1, 10, new byte[10]);
            await store.Store("123", 2, 10, new byte[10]);

            await store.Store("456", 0, 10, new byte[10]);
            await store.Store("456", 1, 10, new byte[10]);

            var result = await store.CountChunks("123");

            result.Should().Be(3);
        }

        [Fact]
        public async Task GetChunks_WithChunksFromMultipleMessages_CorrectChunksReturned()
        {
            var store = new InMemoryChunkStore(new TransactionalListSharedItems<InMemoryTemporaryMessageChunk>());

            await store.Store("123", 0, 10, new byte[10]);
            await store.Store("123", 1, 10, new byte[10]);
            await store.Store("123", 2, 10, new byte[10]);

            await store.Store("456", 0, 10, new byte[20]);
            await store.Store("456", 1, 10, new byte[20]);

            var result = await store.GetChunks("123");

            result.Count.Should().Be(3);
            result.ForEach(chunk => chunk.Value.Length.Should().Be(10));
        }

        [Fact]
        public async Task GetChunks_CommittedChunksStoredFromMultipleInstances_AllChunksReturned()
        {
            var sharedList = new TransactionalListSharedItems<InMemoryTemporaryMessageChunk>();

            var store = new InMemoryChunkStore(sharedList);
            await store.Store("123", 0, 10, new byte[10]);
            await store.Store("123", 1, 10, new byte[10]);
            await store.Commit();

            store = new InMemoryChunkStore(sharedList);
            await store.Store("123", 2, 10, new byte[10]);
            await store.Commit();

            store = new InMemoryChunkStore(sharedList);
            var result = await store.GetChunks("123");

            result.Count.Should().Be(3);
        }

        [Fact]
        public async Task GetChunks_UncommittedChunksStoredFromMultipleInstances_OnlyCurrentScopeChunksReturned()
        {
            var sharedList = new TransactionalListSharedItems<InMemoryTemporaryMessageChunk>();

            var store = new InMemoryChunkStore(sharedList);
            await store.Store("123", 0, 10, new byte[10]);
            await store.Store("123", 1, 10, new byte[10]);

            store = new InMemoryChunkStore(sharedList);
            await store.Store("123", 2, 10, new byte[10]);

            var result = await store.GetChunks("123");

            result.Count.Should().Be(1);
        }

        [Fact]
        public async Task CleanupMessage_WithCommittedAndUncommittedChunksFromMultipleMessages_CorrectChunksRemoved()
        {
            var store = new InMemoryChunkStore(new TransactionalListSharedItems<InMemoryTemporaryMessageChunk>());

            await store.Store("123", 0, 10, new byte[10]);
            await store.Store("123", 1, 10, new byte[10]);
            await store.Store("123", 2, 10, new byte[10]);
            await store.Store("456", 0, 10, new byte[20]);
            await store.Store("456", 1, 10, new byte[20]);
            await store.Commit();
            await store.Store("123", 3, 10, new byte[10]);
            await store.Store("123", 4, 10, new byte[10]);
            await store.Store("456", 2, 10, new byte[20]);

            await store.Cleanup("123");
            await store.Commit();

            store.CommittedItemsCount.Should().Be(3);
            (await store.GetChunks("123")).Count.Should().Be(0);
        }

        [Fact]
        public async Task Commit_StoreAndCleanup_Committed()
        {
            var store = new InMemoryChunkStore(new TransactionalListSharedItems<InMemoryTemporaryMessageChunk>());

            await store.Store("123", 0, 10, new byte[10]);
            await store.Commit();

            store.CommittedItemsCount.Should().Be(1);

            await store.Cleanup("123");
            await store.Commit();

            store.CommittedItemsCount.Should().Be(0);
        }

        [Fact]
        public async Task Rollback_StoreAndCleanup_Reverted()
        {
            var store = new InMemoryChunkStore(new TransactionalListSharedItems<InMemoryTemporaryMessageChunk>());

            await store.Store("123", 0, 10, new byte[10]);
            await store.Commit();

            store.CommittedItemsCount.Should().Be(1);

            await store.Store("123", 1, 10, new byte[10]);
            await store.Rollback();

            store.CommittedItemsCount.Should().Be(1);

            await store.Cleanup("123");
            await store.Rollback();

            store.CommittedItemsCount.Should().Be(1);
        }

        [Fact]
        public async Task HasNotPersistedChunks_CommittedChunks_TrueReturned()
        {
            var store = new InMemoryChunkStore(new TransactionalListSharedItems<InMemoryTemporaryMessageChunk>());

            await store.Store("123", 0, 10, new byte[10]);
            await store.Commit();
            store.HasNotPersistedChunks.Should().BeTrue();
        }

        [Fact]
        public async Task HasNotPersistedChunks_UncommittedChunks_TrueReturned()
        {
            var store = new InMemoryChunkStore(new TransactionalListSharedItems<InMemoryTemporaryMessageChunk>());

            await store.Store("123", 0, 10, new byte[10]);
            store.HasNotPersistedChunks.Should().BeTrue();
        }

        [Fact]
        public async Task HasNotPersistedChunks_UncommittedCleanup_FalseReturned()
        {
            var store = new InMemoryChunkStore(new TransactionalListSharedItems<InMemoryTemporaryMessageChunk>());

            await store.Store("123", 0, 10, new byte[10]);
            await store.Commit();
            await store.Store("123", 1, 10, new byte[10]);
            store.HasNotPersistedChunks.Should().BeTrue();

            await store.Cleanup("123");
            store.HasNotPersistedChunks.Should().BeFalse();
        }

        [Fact]
        public async Task CleanupByAge_CommittedChunks_OlderChunksRemoved()
        {
            var store = new InMemoryChunkStore(new TransactionalListSharedItems<InMemoryTemporaryMessageChunk>());

            await store.Store("123", 0, 10, new byte[10]);
            await store.Store("123", 1, 10, new byte[10]);
            await store.Store("123", 2, 10, new byte[10]);
            await store.Commit();

            await Task.Delay(100);
            var threshold = DateTime.UtcNow;

            await store.Store("456", 0, 10, new byte[20]);
            await store.Store("456", 1, 10, new byte[20]);
            await store.Commit();

            await store.Cleanup(threshold);

            store.CommittedItemsCount.Should().Be(2);
            (await store.GetChunks("123")).Count.Should().Be(0);
        }
    }
}