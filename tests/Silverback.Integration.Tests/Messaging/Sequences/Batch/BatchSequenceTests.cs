// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NSubstitute;
using Shouldly;
using Silverback.Configuration;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Consuming.Transaction;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Messaging.Sequences.Batch;
using Silverback.Tests.Logging;
using Silverback.Tests.Types;
using Silverback.Tests.Types.Domain;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences.Batch;

public class BatchSequenceTests
{
    private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    [Theory]
    [InlineData(SequenceAbortReason.EnumerationAborted, true)]
    [InlineData(SequenceAbortReason.ConsumerAborted, false)]
    [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Awaited before the disposal")]
    public async Task AbortAsync_ShouldRemoveFromStoreAfterTransactionOnlyIfCommitted(
        SequenceAbortReason abortReason,
        bool isKeptInStoreDuringTransaction)
    {
        await using SequenceStore store = new(new SilverbackLoggerSubstitute<SequenceStore>());
        TaskCompletionSource<bool> transactionStarted = new(TaskCreationOptions.RunContinuationsAsynchronously);
        TaskCompletionSource<bool> releaseTransaction = new(TaskCreationOptions.RunContinuationsAsynchronously);
        IConsumerTransactionManager transactionManager = Substitute.For<IConsumerTransactionManager>();
        transactionManager.CommitAsync().Returns(_ =>
        {
            transactionStarted.TrySetResult(true);
            return releaseTransaction.Task;
        });
        transactionManager.RollbackAsync(Arg.Any<Exception?>(), Arg.Any<bool>(), Arg.Any<bool>(), Arg.Any<bool>()).Returns(_ =>
        {
            transactionStarted.TrySetResult(true);
            return releaseTransaction.Task;
        });

        TestConsumerEndpointConfiguration endpointConfiguration = new("test")
        {
            Batch = new BatchSettings
            {
                Size = 10
            }
        };
        InboundEnvelope envelope = new(
            "message",
            Stream.Null,
            null,
            new TestConsumerEndpoint("test", endpointConfiguration),
            Substitute.For<IConsumer>(),
            new TestOffset());
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(
            envelope,
            transactionManager: transactionManager,
            sequenceStore: store);
        using BatchSequence batch = await store.AddAsync(new BatchSequence("batch-1", context));

        // The abort commits the batch whose subscriber stopped enumerating it, while the stop rolls it back
        Task abortTask = batch.AbortAsync(abortReason);
        BatchSequence? storedBatch;

        try
        {
            await transactionStarted.Task.WaitAsync(Timeout);

            // The next message waits for the batch in the store: a committed batch is kept until the commit completes, so that the next
            // batch can't be committed first
            storedBatch = await store.GetAsync<BatchSequence>("batch-1");
            storedBatch.ShouldBe(isKeptInStoreDuringTransaction ? batch : null);
        }
        finally
        {
            // Released anyway, since the disposal waits for the abort
            releaseTransaction.TrySetResult(true);
        }

        await abortTask.WaitAsync(Timeout);

        storedBatch = await store.GetAsync<BatchSequence>("batch-1");
        storedBatch.ShouldBeNull();
    }

    [Fact]
    public async Task OnTimeoutElapsedAsync_ShouldNotAbortBatch_WhenStreamsWereAbortedByPublisher()
    {
        TestConsumerEndpointConfiguration endpointConfiguration = new("test")
        {
            Batch = new BatchSettings
            {
                Size = 10,
                MaxWaitTime = TimeSpan.FromMinutes(1)
            }
        };
        InboundEnvelope envelope = new(
            "message",
            Stream.Null,
            null,
            new TestConsumerEndpoint("test", endpointConfiguration),
            Substitute.For<IConsumer>(),
            new TestOffset());
        using ConsumerPipelineContext context = ConsumerPipelineContextHelper.CreateSubstitute(envelope);
        using TimeoutTestBatchSequence batch = new("batch-1", context);

        // One of several subscribers failed: the publisher aborts the streams first, and the batch (with the exception of the subscriber)
        // only once the other subscribers returned
        ((MessageStreamProvider)batch.StreamProvider).AbortIfPending();

        await batch.ElapseTimeoutFromTestAsync().WaitAsync(Timeout);

        // Not aborted with the exception of the failed completion, to which the error policies would be applied
        batch.IsPending.ShouldBeTrue();
        batch.AbortReason.ShouldBe(SequenceAbortReason.None);
        batch.AbortException.ShouldBeNull();
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenBatchIsNotEnabled()
    {
        Action act = () =>
        {
            using ConsumerPipelineContext context = CreateContext(null);
            using BatchSequence dummy = new("batch-1", context);
        };

        act.ShouldThrow<InvalidOperationException>().Message.ShouldBe("Endpoint.Batch is null.");
    }

    [Fact]
    public async Task CompleteEarlyAsync_ShouldCompleteBatchBeforeItIsFull()
    {
        using ConsumerPipelineContext context = CreateContext(new BatchSettings { Size = 10 });
        using BatchSequence batch = new("batch-1", context);
        Task<List<IInboundEnvelope>> enumerationTask = EnumerateAsync(batch);
        InboundEnvelope<TestEventOne> envelope1 = CreateEnvelope();
        InboundEnvelope<TestEventOne> envelope2 = CreateEnvelope();
        (await batch.AddAsync(envelope1, null, true).WaitAsync(Timeout)).IsSuccess.ShouldBeTrue();
        (await batch.AddAsync(envelope2, null, true).WaitAsync(Timeout)).IsSuccess.ShouldBeTrue();

        // E.g. the consumer caught up with the end of the partition
        await batch.CompleteEarlyAsync().WaitAsync(Timeout);

        List<IInboundEnvelope> enumeratedEnvelopes = await enumerationTask.WaitAsync(Timeout);
        enumeratedEnvelopes.ShouldBe([envelope1, envelope2]);
        batch.IsComplete.ShouldBeTrue();
        batch.IsAborted.ShouldBeFalse();
        batch.Length.ShouldBe(2);
    }

    [Fact]
    public async Task OnTimeoutElapsedAsync_ShouldCompleteBatch()
    {
        using ConsumerPipelineContext context = CreateContext(
            new BatchSettings
            {
                Size = 10,
                MaxWaitTime = TimeSpan.FromMinutes(1)
            });
        using TimeoutTestBatchSequence batch = new("batch-1", context);
        Task<List<IInboundEnvelope>> enumerationTask = EnumerateAsync(batch);
        InboundEnvelope<TestEventOne> envelope = CreateEnvelope();
        (await batch.AddAsync(envelope, null, true).WaitAsync(Timeout)).IsSuccess.ShouldBeTrue();

        await batch.ElapseTimeoutFromTestAsync().WaitAsync(Timeout);

        // Completed, instead of being aborted as incomplete like the other sequences timing out
        List<IInboundEnvelope> enumeratedEnvelopes = await enumerationTask.WaitAsync(Timeout);
        enumeratedEnvelopes.ShouldBe([envelope]);
        batch.IsComplete.ShouldBeTrue();
        batch.AbortReason.ShouldBe(SequenceAbortReason.None);
        batch.Length.ShouldBe(1);
    }

    [Fact]
    public async Task AddAsync_ShouldNotExtendMaxWaitTime_WhenFurtherMessagesAreAdded()
    {
        using ConsumerPipelineContext context = CreateContext(
            new BatchSettings
            {
                Size = 10,
                MaxWaitTime = TimeSpan.FromMinutes(1)
            });
        using BatchSequence batch = new("batch-1", context);
        Task<List<IInboundEnvelope>> enumerationTask = EnumerateAsync(batch);

        (await batch.AddAsync(CreateEnvelope(), null, true).WaitAsync(Timeout)).IsSuccess.ShouldBeTrue();
        DateTime firstExpiration = GetTimeoutExpiration(batch);

        // Make sure that the clock moved on, since a sliding timeout would then expire later
        DateTime firstMessageTime = DateTime.UtcNow;
        await AsyncTestingUtil.WaitAsync(() => DateTime.UtcNow > firstMessageTime);
        (await batch.AddAsync(CreateEnvelope(), null, true).WaitAsync(Timeout)).IsSuccess.ShouldBeTrue();

        // The max wait time runs from the first message
        GetTimeoutExpiration(batch).ShouldBe(firstExpiration);
        firstExpiration.ShouldBeLessThanOrEqualTo(firstMessageTime.AddMinutes(1));

        await batch.CompleteEarlyAsync().WaitAsync(Timeout);
        (await enumerationTask.WaitAsync(Timeout)).Count.ShouldBe(2);
    }

    [Theory]
    [InlineData("timeout")]
    [InlineData("early")]
    public async Task CompleteAsync_ShouldAbortBatchWithError_WhenCompletionFails(string trigger)
    {
        IServiceProvider serviceProvider = ServiceProviderHelper.GetScopedServiceProvider(services => services
            .AddLoggerSubstitute()
            .AddSilverback()
            .WithConnectionToMessageBroker());
        using ConsumerPipelineContext context = CreateContext(
            new BatchSettings
            {
                Size = 10,
                MaxWaitTime = TimeSpan.FromMinutes(1)
            },
            serviceProvider);
        InvalidOperationException exception = new("Test completion failure");
        using FailingCompletionBatchSequence batch = new("batch-1", context, exception);

        Task completeTask = trigger == "timeout" ? batch.ElapseTimeoutFromTestAsync() : batch.CompleteEarlyAsync();
        await completeTask.WaitAsync(Timeout);

        // The streams aren't aborted, so the failure isn't ignored but logged and handled as a processing error
        batch.IsAborted.ShouldBeTrue();
        batch.AbortReason.ShouldBe(SequenceAbortReason.Error);
        batch.AbortException.ShouldBeSameAs(exception);
        LoggerSubstitute<BatchSequence> logger =
            (LoggerSubstitute<BatchSequence>)serviceProvider.GetRequiredService<ILogger<BatchSequence>>();
        logger.Received(
            LogLevel.Warning,
            typeof(InvalidOperationException),
            "Error occurred executing timeout for FailingCompletionBatchSequence batch-1",
            1018);
    }

    private static ConsumerPipelineContext CreateContext(BatchSettings? batchSettings, IServiceProvider? serviceProvider = null)
    {
        TestConsumerEndpointConfiguration endpointConfiguration = new("test")
        {
            Batch = batchSettings
        };
        InboundEnvelope envelope = new(
            "message",
            Stream.Null,
            null,
            new TestConsumerEndpoint("test", endpointConfiguration),
            Substitute.For<IConsumer>(),
            new TestOffset());
        return ConsumerPipelineContextHelper.CreateSubstitute(envelope, serviceProvider);
    }

    private static InboundEnvelope<TestEventOne> CreateEnvelope() =>
        new(
            new TestEventOne(),
            Stream.Null,
            [],
            TestConsumerEndpoint.GetDefault(),
            Substitute.For<IConsumer>(),
            new TestOffset());

    private static Task<List<IInboundEnvelope>> EnumerateAsync(BatchSequence batch)
    {
        IMessageStreamEnumerable<IInboundEnvelope> stream = batch.CreateStream<IInboundEnvelope>();

        return Task.Run(async () =>
        {
            List<IInboundEnvelope> envelopes = [];

            await foreach (IInboundEnvelope envelope in stream)
            {
                envelopes.Add(envelope);
            }

            return envelopes;
        });
    }

    private static DateTime GetTimeoutExpiration(BatchSequence batch) =>
        (DateTime)typeof(SequenceBase<IInboundEnvelope>)
            .GetField("_timeoutExpiration", BindingFlags.Instance | BindingFlags.NonPublic)!
            .GetValue(batch)!;

    private sealed class TimeoutTestBatchSequence : BatchSequence
    {
        public TimeoutTestBatchSequence(string sequenceId, ConsumerPipelineContext context)
            : base(sequenceId, context)
        {
        }

        public Task ElapseTimeoutFromTestAsync() => OnTimeoutElapsedAsync();
    }

    private sealed class FailingCompletionBatchSequence : BatchSequence
    {
        private readonly Exception _exception;

        public FailingCompletionBatchSequence(string sequenceId, ConsumerPipelineContext context, Exception exception)
            : base(sequenceId, context)
        {
            _exception = exception;
        }

        public Task ElapseTimeoutFromTestAsync() => OnTimeoutElapsedAsync();

        protected override Task CompleteAsync(CancellationToken cancellationToken = default) => Task.FromException(_exception);
    }
}
