// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Sequences;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences;

public class SequenceReaderBaseTests
{
    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public async Task GetSequenceAsync_ShouldThrow_WhenSequenceIdIsNullOrEmpty(string? sequenceId)
    {
        ISequenceStore sequenceStore = Substitute.For<ISequenceStore>();
        TestSequenceReader reader = new(sequenceId);
        ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(sequenceStore: sequenceStore);

        Exception exception = await Should.ThrowAsync<InvalidOperationException>(() => reader.GetSequenceAsync(context).AsTask());

        exception.Message.ShouldBe("Sequence identifier not found or invalid.");
        sequenceStore.ReceivedCalls().ShouldBeEmpty();
    }

    private sealed class TestSequenceReader : SequenceReaderBase
    {
        private readonly string? _sequenceId;

        public TestSequenceReader(string? sequenceId)
        {
            _sequenceId = sequenceId;
        }

        public override ValueTask<bool> CanHandleAsync(ConsumerPipelineContext context) => ValueTask.FromResult(true);

        protected override ValueTask<string> GetSequenceIdAsync(ConsumerPipelineContext context) => ValueTask.FromResult(_sequenceId!);

        protected override ValueTask<bool> IsNewSequenceAsync(string sequenceId, ConsumerPipelineContext context) =>
            ValueTask.FromResult(true);

        protected override ISequence CreateNewSequenceCore(string sequenceId, ConsumerPipelineContext context) =>
            throw new InvalidOperationException("Unexpected new sequence.");
    }
}
