// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Chunking;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences
{
    public class DefaultSequenceStoreTests
    {
        private readonly ConsumerPipelineContext _consumerPipelineContext =
            ConsumerPipelineContextHelper.CreateSubstitute();

        [Fact]
        public async Task Get_ExistingSequence_SequenceReturned()
        {
            var store = new DefaultSequenceStore();

            await store.AddAsync(new ChunkSequence("aaa", 10, _consumerPipelineContext, store));
            await store.AddAsync(new ChunkSequence("bbb", 10, _consumerPipelineContext, store));
            await store.AddAsync(new ChunkSequence("ccc", 10, _consumerPipelineContext, store));

            var result = await store.GetAsync<ChunkSequence>("bbb");

            result.Should().NotBeNull();
            result!.SequenceId.Should().Be("bbb");
        }

        [Fact]
        public async Task Get_NotExistingSequence_NullReturned()
        {
            var store = new DefaultSequenceStore();

            await store.AddAsync(new ChunkSequence("aaa", 10, _consumerPipelineContext, store));
            await store.AddAsync(new ChunkSequence("bbb", 10, _consumerPipelineContext, store));
            await store.AddAsync(new ChunkSequence("ccc", 10, _consumerPipelineContext, store));

            var result = await store.GetAsync<ChunkSequence>("123");

            result.Should().BeNull();
        }

        [Fact]
        public async Task Add_NewSequence_SequenceAddedAndReturned()
        {
            var store = new DefaultSequenceStore();

            var newSequence = new ChunkSequence("abc", 10, _consumerPipelineContext, store);
            var result = await store.AddAsync(newSequence);

            result.Should().BeSameAs(newSequence);
            (await store.GetAsync<ChunkSequence>("abc")).Should().BeSameAs(newSequence);
        }

        [Fact]
        public async Task Add_ExistingSequence_SequenceAbortedAndReplaced()
        {
            var store = new DefaultSequenceStore();

            var originalSequence = new ChunkSequence("abc", 10, _consumerPipelineContext, store);
            await store.AddAsync(originalSequence);

            var newSequence = new ChunkSequence("abc", 10, _consumerPipelineContext, store);
            await store.AddAsync(newSequence);

            originalSequence.IsAborted.Should().BeTrue();

            (await store.GetAsync<ChunkSequence>("abc")).Should().BeSameAs(newSequence);
        }

        [Fact]
        public async Task AddAndGet_Sequence_IsNewFlagAutomaticallyHandled()
        {
            var store = new DefaultSequenceStore();

            var sequence = await store.AddAsync(new ChunkSequence("abc", 10, _consumerPipelineContext, store));

            sequence.IsNew.Should().BeTrue();

            sequence = await store.GetAsync<ChunkSequence>("abc");

            sequence!.IsNew.Should().BeFalse();
        }

        [Fact]
        public async Task Remove_ExistingSequence_SequenceRemoved()
        {
            var store = new DefaultSequenceStore();

            await store.AddAsync(new ChunkSequence("aaa", 10, _consumerPipelineContext, store));
            await store.AddAsync(new ChunkSequence("bbb", 10, _consumerPipelineContext, store));
            await store.AddAsync(new ChunkSequence("ccc", 10, _consumerPipelineContext, store));

            await store.RemoveAsync("bbb");

            (await store.GetAsync<ChunkSequence>("bbb")).Should().BeNull();
            (await store.GetAsync<ChunkSequence>("aaa")).Should().NotBeNull();
            (await store.GetAsync<ChunkSequence>("ccc")).Should().NotBeNull();
        }

        [Fact]
        public async Task Remove_NotExistingSequence_NoExceptionThrown()
        {
            var store = new DefaultSequenceStore();

            await store.AddAsync(new ChunkSequence("aaa", 10, _consumerPipelineContext, store));
            await store.AddAsync(new ChunkSequence("bbb", 10, _consumerPipelineContext, store));
            await store.AddAsync(new ChunkSequence("ccc", 10, _consumerPipelineContext, store));

            await store.RemoveAsync("123");

            (await store.GetAsync<ChunkSequence>("aaa")).Should().NotBeNull();
            (await store.GetAsync<ChunkSequence>("bbb")).Should().NotBeNull();
            (await store.GetAsync<ChunkSequence>("ccc")).Should().NotBeNull();
        }
    }
}
