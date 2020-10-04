// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Chunking;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences
{
    public class DefaultSequenceStoreTests
    {
        private readonly ConsumerPipelineContext _consumerPipelineContext = ConsumerPipelineContextHelper.CreateSubstitute();

        [Fact]
        public void Get_ExistingSequence_SequenceReturned()
        {
            var store = new DefaultSequenceStore<ChunkSequence>();

            store.Add(new ChunkSequence("aaa", 10, _consumerPipelineContext));
            store.Add(new ChunkSequence("bbb", 10, _consumerPipelineContext));
            store.Add(new ChunkSequence("ccc", 10, _consumerPipelineContext));

            var result = store.Get("bbb");

            result.Should().NotBeNull();
            result!.SequenceId.Should().Be("bbb");
        }

        [Fact]
        public void Get_NotExistingSequence_NullReturned()
        {
            var store = new DefaultSequenceStore<ChunkSequence>();

            store.Add(new ChunkSequence("aaa", 10, _consumerPipelineContext));
            store.Add(new ChunkSequence("bbb", 10, _consumerPipelineContext));
            store.Add(new ChunkSequence("ccc", 10, _consumerPipelineContext));

            var result = store.Get("123");

            result.Should().BeNull();
        }

        [Fact]
        public void Add_NewSequence_SequenceAddedAndReturned()
        {
            var store = new DefaultSequenceStore<ChunkSequence>();

            var newSequence = new ChunkSequence("abc", 10, _consumerPipelineContext);
            var result = store.Add(newSequence);

            result.Should().BeSameAs(newSequence);
            store.Get("abc").Should().BeSameAs(newSequence);
        }

        [Fact]
        public void Add_ExistingSequence_ExceptionThrown()
        {
            var store = new DefaultSequenceStore<ChunkSequence>();

            store.Add(new ChunkSequence("abc", 10, _consumerPipelineContext));

            Action act = () => store.Add(new ChunkSequence("abc", 10, _consumerPipelineContext));
            act.Should().Throw<InvalidOperationException>();
        }

        [Fact]
        public void Remove_ExistingSequence_SequenceRemoved()
        {
            var store = new DefaultSequenceStore<ChunkSequence>();

            store.Add(new ChunkSequence("aaa", 10, _consumerPipelineContext));
            store.Add(new ChunkSequence("bbb", 10, _consumerPipelineContext));
            store.Add(new ChunkSequence("ccc", 10, _consumerPipelineContext));

            store.Remove("bbb");

            store.Get("bbb").Should().BeNull();
            store.Get("aaa").Should().NotBeNull();
            store.Get("ccc").Should().NotBeNull();
        }

        [Fact]
        public void Remove_NotExistingSequence_NoExceptionThrown()
        {
            var store = new DefaultSequenceStore<ChunkSequence>();

            store.Add(new ChunkSequence("aaa", 10, _consumerPipelineContext));
            store.Add(new ChunkSequence("bbb", 10, _consumerPipelineContext));
            store.Add(new ChunkSequence("ccc", 10, _consumerPipelineContext));

            store.Remove("123");

            store.Get("aaa").Should().NotBeNull();
            store.Get("bbb").Should().NotBeNull();
            store.Get("ccc").Should().NotBeNull();
        }
    }
}
