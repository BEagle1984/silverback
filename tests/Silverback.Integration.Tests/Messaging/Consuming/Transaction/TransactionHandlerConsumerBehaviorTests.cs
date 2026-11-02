// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Consuming.Transaction;

public class TransactionHandlerConsumerBehaviorTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Fact]
    public async Task HandleAsync_ShouldCommitLastPulledMessage_WhenEnumerationStopsBeforeSequenceIsComplete()
    {
        TestOffset offset1 = new("test", "1");
        TestOffset offset2 = new("test", "2");
        using ConsumerPipelineContext context = CreateContext(offset1);
        TestSequence sequence = new(context, 10);
        IMessageStreamEnumerable<IRawInboundEnvelope> stream = sequence.CreateStream<IRawInboundEnvelope>();
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        // Stop enumerating after pulling the second message: its push completes only when the next message is pulled, so it's
        // still pending when the processing completes
        Task processingTask = Task.Run(async () =>
        {
            int count = 0;

            await foreach (IRawInboundEnvelope dummy in stream)
            {
                if (++count == 2)
                    break;
            }
        });

        await behavior.HandleAsync(
            context,
            async (pipelineContext, _) =>
            {
                pipelineContext.SetSequence(sequence, true);
                pipelineContext.ProcessingTask = processingTask;
                await sequence.AddAsync(pipelineContext.Envelope, null, true);
            },
            default).AsTask().WaitAsync(Timeout);
        Task<AddToSequenceResult> addTask = sequence.AddAsync(CreateEnvelope(offset2), null, true);

        await sequence.ProcessingCompletedTask.WaitAsync(Timeout);
        AddToSequenceResult addResult = await addTask.WaitAsync(Timeout);
        await sequence.DisposedTask.WaitAsync(Timeout);

        await context.Consumer.Received(1).CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await context.Consumer.Received(1).CommitAsync(
            Arg.Is<IReadOnlyCollection<IBrokerMessageIdentifier>>(identifiers => identifiers.ToHashSet().SetEquals(new[] { offset1, offset2 })));
        sequence.IsAborted.ShouldBeTrue();
        sequence.AbortReason.ShouldBe(SequenceAbortReason.EnumerationAborted);

        // Reported as aborted and removed from the store, otherwise the sequencer would retry adding to the disposed sequence
        addResult.IsAborted.ShouldBeTrue();
        await context.SequenceStore.Received(1).RemoveAsync(sequence.SequenceId);
    }

    [Theory]
    [InlineData(true, false, false, false, false)]
    [InlineData(false, false, false, true, false)]
    [InlineData(true, true, false, false, true)]
    [InlineData(false, false, true, false, true)]
    [SuppressMessage("Reliability", "CA2012:Use ValueTasks correctly", Justification = "NSubstitute setup")]
    public async Task HandleAsync_ShouldCommitOrAbort_WhenProcessingCompleted(
        bool isPending,
        bool isCompleting,
        bool isComplete,
        bool isAborted,
        bool shouldCommit)
    {
        using ConsumerPipelineContext context = CreateContext(new TestOffset());
        ISequence sequence = Substitute.For<ISequence>();
        sequence.Context.Returns(context);
        sequence.ParentSequence.Returns((ISequence?)null);
        sequence.IsPending.Returns(isPending);
        sequence.IsCompleting.Returns(isCompleting);
        sequence.IsComplete.Returns(isComplete);
        sequence.IsAborted.Returns(isAborted);
        TaskCompletionSource disposedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);
        sequence.When(substitute => substitute.Dispose()).Do(_ => disposedTaskCompletionSource.TrySetResult());
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        await behavior.HandleAsync(
            context,
            (pipelineContext, _) =>
            {
                pipelineContext.SetSequence(sequence, true);
                pipelineContext.ProcessingTask = Task.CompletedTask;
                return ValueTask.CompletedTask;
            },
            default).AsTask().WaitAsync(Timeout);

        await disposedTaskCompletionSource.Task.WaitAsync(Timeout);

        await context.Consumer.Received(shouldCommit ? 1 : 0).CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await sequence.Received(shouldCommit ? 0 : 1).AbortAsync(SequenceAbortReason.EnumerationAborted);
    }

    [Fact]
    public async Task HandleAsync_ShouldCommitAllMessages_WhenSequenceIsComplete()
    {
        TestOffset offset1 = new("test", "1");
        TestOffset offset2 = new("test", "2");
        using ConsumerPipelineContext context = CreateContext(offset1);
        TestSequence sequence = new(context, 2);
        IMessageStreamEnumerable<IRawInboundEnvelope> stream = sequence.CreateStream<IRawInboundEnvelope>();
        TransactionHandlerConsumerBehavior behavior = new(Substitute.For<ISilverbackLogger<TransactionHandlerConsumerBehavior>>());

        Task processingTask = Task.Run(async () =>
        {
            await foreach (IRawInboundEnvelope dummy in stream)
            {
                // Just consume the stream
            }
        });

        await behavior.HandleAsync(
            context,
            async (pipelineContext, _) =>
            {
                pipelineContext.SetSequence(sequence, true);
                pipelineContext.ProcessingTask = processingTask;
                await sequence.AddAsync(pipelineContext.Envelope, null, true);
            },
            default).AsTask().WaitAsync(Timeout);
        AddToSequenceResult addResult = await sequence.AddAsync(CreateEnvelope(offset2), null, true).WaitAsync(Timeout);

        await sequence.ProcessingCompletedTask.WaitAsync(Timeout);

        addResult.IsSuccess.ShouldBeTrue();
        sequence.IsComplete.ShouldBeTrue();
        await context.Consumer.Received(1).CommitAsync(Arg.Any<IReadOnlyCollection<IBrokerMessageIdentifier>>());
        await context.Consumer.Received(1).CommitAsync(
            Arg.Is<IReadOnlyCollection<IBrokerMessageIdentifier>>(identifiers => identifiers.ToHashSet().SetEquals(new[] { offset1, offset2 })));
    }

    // Not using the ConsumerPipelineContextHelper, since the behavior initializes the transaction manager
    private static ConsumerPipelineContext CreateContext(IBrokerMessageIdentifier identifier)
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services =>
            services
                .AddSingleton(Substitute.For<IHostApplicationLifetime>())
                .AddFakeLogger()
                .AddSilverback()
                .WithConnectionToMessageBroker());

        return new ConsumerPipelineContext(
            CreateEnvelope(identifier),
            Substitute.For<IConsumer>(),
            Substitute.For<ISequenceStore>(),
            [],
            serviceProvider);
    }

    private static RawInboundEnvelope CreateEnvelope(IBrokerMessageIdentifier identifier) =>
        new(
            Stream.Null,
            [],
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            identifier);

    private sealed class TestSequence : SequenceBase<IRawInboundEnvelope>
    {
        private readonly TaskCompletionSource _disposedTaskCompletionSource = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public TestSequence(ConsumerPipelineContext context, int totalLength)
            : base("test-sequence", context, enforceTimeout: false, trackIdentifiers: true)
        {
            TotalLength = totalLength;
        }

        public Task DisposedTask => _disposedTaskCompletionSource.Task;

        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);

            if (disposing)
                _disposedTaskCompletionSource.TrySetResult();
        }
    }
}
