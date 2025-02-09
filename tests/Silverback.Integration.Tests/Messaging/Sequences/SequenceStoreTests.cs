// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Chunking;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences;

public class SequenceStoreTests
{
    [Fact]
    public async Task GetAsync_ExistingSequence_SequenceReturned()
    {
        SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);

        await store.AddAsync(new ChunkSequence("aaa", 10, context));
        await store.AddAsync(new ChunkSequence("bbb", 10, context));
        await store.AddAsync(new ChunkSequence("ccc", 10, context));

        ChunkSequence? result = await store.GetAsync<ChunkSequence>("bbb");

        result.ShouldNotBeNull();
        result.SequenceId.ShouldBe("bbb");
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task GetAsync_PartialId_SequenceReturnedIfMatchPrefixIsTrue(bool matchPrefix)
    {
        SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);

        await store.AddAsync(new ChunkSequence("aaa-123", 10, context));

        ChunkSequence? result = await store.GetAsync<ChunkSequence>("aaa", matchPrefix);

        if (matchPrefix)
        {
            result.ShouldNotBeNull();
            result.SequenceId.ShouldBe("aaa-123");
        }
        else
        {
            result.ShouldBeNull();
        }
    }

    [Fact]
    public async Task GetAsync_NotExistingSequence_NullReturned()
    {
        SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);

        await store.AddAsync(new ChunkSequence("aaa", 10, context));
        await store.AddAsync(new ChunkSequence("bbb", 10, context));
        await store.AddAsync(new ChunkSequence("ccc", 10, context));

        ChunkSequence? result = await store.GetAsync<ChunkSequence>("123");

        result.ShouldBeNull();
    }

    [Fact]
    public async Task AddAsync_NewSequence_SequenceAddedAndReturned()
    {
        SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);

        ChunkSequence newSequence = new("abc", 10, context);
        ChunkSequence result = await store.AddAsync(newSequence);

        result.ShouldBeSameAs(newSequence);
        (await store.GetAsync<ChunkSequence>("abc")).ShouldBeSameAs(newSequence);
    }

    [Fact]
    public async Task AddAsync_ExistingSequence_SequenceAbortedAndReplaced()
    {
        SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);

        ChunkSequence originalSequence = new("abc", 10, context);
        await store.AddAsync(originalSequence);

        ChunkSequence newSequence = new("abc", 10, context);
        await store.AddAsync(newSequence);

        originalSequence.IsAborted.ShouldBeTrue();

        (await store.GetAsync<ChunkSequence>("abc")).ShouldBeSameAs(newSequence);
    }

    [Fact]
    public async Task AddAsyncAndGetAsync_Sequence_IsNewFlagAutomaticallyHandled()
    {
        SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);

        ChunkSequence sequence = await store.AddAsync(new ChunkSequence("abc", 10, context));

        sequence.IsNew.ShouldBeTrue();

        sequence = (await store.GetAsync<ChunkSequence>("abc"))!;

        sequence.IsNew.ShouldBeFalse();
    }

    [Fact]
    public async Task Remove_ExistingSequence_SequenceRemoved()
    {
        SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);

        await store.AddAsync(new ChunkSequence("aaa", 10, context));
        await store.AddAsync(new ChunkSequence("bbb", 10, context));
        await store.AddAsync(new ChunkSequence("ccc", 10, context));

        await store.RemoveAsync("bbb");

        (await store.GetAsync<ChunkSequence>("bbb")).ShouldBeNull();
        (await store.GetAsync<ChunkSequence>("aaa")).ShouldNotBeNull();
        (await store.GetAsync<ChunkSequence>("ccc")).ShouldNotBeNull();
    }

    [Fact]
    public async Task Remove_NotExistingSequence_NoExceptionThrown()
    {
        SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: store);

        await store.AddAsync(new ChunkSequence("aaa", 10, context));
        await store.AddAsync(new ChunkSequence("bbb", 10, context));
        await store.AddAsync(new ChunkSequence("ccc", 10, context));

        await store.RemoveAsync("123");

        (await store.GetAsync<ChunkSequence>("aaa")).ShouldNotBeNull();
        (await store.GetAsync<ChunkSequence>("bbb")).ShouldNotBeNull();
        (await store.GetAsync<ChunkSequence>("ccc")).ShouldNotBeNull();
    }

    [Fact]
    public void GetPendingSequences_EmptyStore_EmptyCollectionReturned()
    {
        SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());

        IReadOnlyCollection<ISequence> result = store.GetPendingSequences();

        result.ShouldBeEmpty();
    }

    [Fact]
    public async Task GetPendingSequences_WithIncompleteSequence_PendingSequencesReturned()
    {
        SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());

        await store.AddAsync(new FakeSequence("aaa", true, false, store));
        await store.AddAsync(new FakeSequence("bbb", false, true, store));
        await store.AddAsync(new FakeSequence("ccc", false, false, store));
        await store.AddAsync(new FakeSequence("ddd", false, false, store));

        IReadOnlyCollection<ISequence> result = store.GetPendingSequences();

        result.Count.ShouldBe(2);
        result.Select(sequence => sequence.SequenceId).ShouldBe(["ccc", "ddd"]);
    }

    [Fact]
    public async Task GetPendingSequences_WithAllCompleteOrAbortedSequences_EmptyCollectionReturned()
    {
        SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());

        await store.AddAsync(new FakeSequence("aaa", true, false, store));
        await store.AddAsync(new FakeSequence("bbb", false, true, store));

        IReadOnlyCollection<ISequence> result = store.GetPendingSequences();

        result.ShouldBeEmpty();
    }
}
