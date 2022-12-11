// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Tests.Integration.TestTypes;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences;

public class DefaultSequenceStoreTests
{
    [Fact]
    public async Task GetAsync_ExistingSequence_SequenceReturned()
    {
        DefaultSequenceStore store = new(new SilverbackLoggerSubstitute<DefaultSequenceStore>());
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);

        await store.AddAsync(new ChunkSequence("aaa", 10, context));
        await store.AddAsync(new ChunkSequence("bbb", 10, context));
        await store.AddAsync(new ChunkSequence("ccc", 10, context));

        ChunkSequence? result = await store.GetAsync<ChunkSequence>("bbb");

        result.Should().NotBeNull();
        result!.SequenceId.Should().Be("bbb");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetAsync_PartialId_SequenceReturnedIfMatchPrefixIsTrue(bool matchPrefix)
    {
        DefaultSequenceStore store = new(new SilverbackLoggerSubstitute<DefaultSequenceStore>());
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);

        await store.AddAsync(new ChunkSequence("aaa-123", 10, context));

        ChunkSequence? result = await store.GetAsync<ChunkSequence>("aaa", matchPrefix);

        if (matchPrefix)
        {
            result.Should().NotBeNull();
            result!.SequenceId.Should().Be("aaa-123");
        }
        else
        {
            result.Should().BeNull();
        }
    }

    [Fact]
    public async Task GetAsync_NotExistingSequence_NullReturned()
    {
        DefaultSequenceStore store = new(new SilverbackLoggerSubstitute<DefaultSequenceStore>());
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);

        await store.AddAsync(new ChunkSequence("aaa", 10, context));
        await store.AddAsync(new ChunkSequence("bbb", 10, context));
        await store.AddAsync(new ChunkSequence("ccc", 10, context));

        ChunkSequence? result = await store.GetAsync<ChunkSequence>("123");

        result.Should().BeNull();
    }

    [Fact]
    public async Task AddAsync_NewSequence_SequenceAddedAndReturned()
    {
        DefaultSequenceStore store = new(new SilverbackLoggerSubstitute<DefaultSequenceStore>());
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);

        ChunkSequence newSequence = new("abc", 10, context);
        ChunkSequence result = await store.AddAsync(newSequence);

        result.Should().BeSameAs(newSequence);
        (await store.GetAsync<ChunkSequence>("abc")).Should().BeSameAs(newSequence);
    }

    [Fact]
    public async Task AddAsync_ExistingSequence_SequenceAbortedAndReplaced()
    {
        DefaultSequenceStore store = new(new SilverbackLoggerSubstitute<DefaultSequenceStore>());
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);

        ChunkSequence originalSequence = new("abc", 10, context);
        await store.AddAsync(originalSequence);

        ChunkSequence newSequence = new("abc", 10, context);
        await store.AddAsync(newSequence);

        originalSequence.IsAborted.Should().BeTrue();

        (await store.GetAsync<ChunkSequence>("abc")).Should().BeSameAs(newSequence);
    }

    [Fact]
    public async Task AddAsyncAndGetAsync_Sequence_IsNewFlagAutomaticallyHandled()
    {
        DefaultSequenceStore store = new(new SilverbackLoggerSubstitute<DefaultSequenceStore>());
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);

        ChunkSequence sequence = await store.AddAsync(new ChunkSequence("abc", 10, context));

        sequence.IsNew.Should().BeTrue();

        sequence = (await store.GetAsync<ChunkSequence>("abc"))!;

        sequence.IsNew.Should().BeFalse();
    }

    [Fact]
    public async Task Remove_ExistingSequence_SequenceRemoved()
    {
        DefaultSequenceStore store = new(new SilverbackLoggerSubstitute<DefaultSequenceStore>());
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);

        await store.AddAsync(new ChunkSequence("aaa", 10, context));
        await store.AddAsync(new ChunkSequence("bbb", 10, context));
        await store.AddAsync(new ChunkSequence("ccc", 10, context));

        await store.RemoveAsync("bbb");

        (await store.GetAsync<ChunkSequence>("bbb")).Should().BeNull();
        (await store.GetAsync<ChunkSequence>("aaa")).Should().NotBeNull();
        (await store.GetAsync<ChunkSequence>("ccc")).Should().NotBeNull();
    }

    [Fact]
    public async Task Remove_NotExistingSequence_NoExceptionThrown()
    {
        DefaultSequenceStore store = new(new SilverbackLoggerSubstitute<DefaultSequenceStore>());
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);

        await store.AddAsync(new ChunkSequence("aaa", 10, context));
        await store.AddAsync(new ChunkSequence("bbb", 10, context));
        await store.AddAsync(new ChunkSequence("ccc", 10, context));

        await store.RemoveAsync("123");

        (await store.GetAsync<ChunkSequence>("aaa")).Should().NotBeNull();
        (await store.GetAsync<ChunkSequence>("bbb")).Should().NotBeNull();
        (await store.GetAsync<ChunkSequence>("ccc")).Should().NotBeNull();
    }

    [Fact]
    public void GetPendingSequences_EmptyStore_EmptyCollectionReturned()
    {
        DefaultSequenceStore store = new(new SilverbackLoggerSubstitute<DefaultSequenceStore>());

        IReadOnlyCollection<ISequence> result = store.GetPendingSequences();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetPendingSequences_WithIncompleteSequence_PendingSequencesReturned()
    {
        DefaultSequenceStore store = new(new SilverbackLoggerSubstitute<DefaultSequenceStore>());

        await store.AddAsync(new FakeSequence("aaa", true, false, store));
        await store.AddAsync(new FakeSequence("bbb", false, true, store));
        await store.AddAsync(new FakeSequence("ccc", false, false, store));
        await store.AddAsync(new FakeSequence("ddd", false, false, store));

        IReadOnlyCollection<ISequence> result = store.GetPendingSequences();

        result.Should().HaveCount(2);
        result.Select(sequence => sequence.SequenceId).Should().BeEquivalentTo("ccc", "ddd");
    }

    [Fact]
    public async Task GetPendingSequences_WithAllCompleteOrAbortedSequences_EmptyCollectionReturned()
    {
        DefaultSequenceStore store = new(new SilverbackLoggerSubstitute<DefaultSequenceStore>());

        await store.AddAsync(new FakeSequence("aaa", true, false, store));
        await store.AddAsync(new FakeSequence("bbb", false, true, store));

        IReadOnlyCollection<ISequence> result = store.GetPendingSequences();

        result.Should().BeEmpty();
    }
}
