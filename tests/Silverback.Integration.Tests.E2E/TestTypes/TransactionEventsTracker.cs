// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Tests.Integration.E2E.TestTypes;

// Tracks the events raised by the consumer transaction managers (Committing, Committed, Aborting and Aborted), right after the
// TransactionHandlerConsumerBehavior initialized them. It optionally makes the first commits fail, as a Committing handler throwing (e.g.,
// the client-side offset store failing to store the offsets), or as a Committed handler throwing, after the consumer was committed.
public sealed class TransactionEventsTracker : IConsumerBehavior
{
    private readonly List<string> _events = [];

    private readonly bool _isFailingAfterConsumerCommit;

    private int _remainingFailingCommitsCount;

    public TransactionEventsTracker(int failingCommitsCount = 0, bool isFailingAfterConsumerCommit = false)
    {
        _remainingFailingCommitsCount = failingCommitsCount;
        _isFailingAfterConsumerCommit = isFailingAfterConsumerCommit;
    }

    public IReadOnlyList<string> Events
    {
        get
        {
            lock (_events)
            {
                return [.. _events];
            }
        }
    }

    public int SortIndex => BrokerBehaviorsSortIndexes.Consumer.TransactionHandler + 1;

    public ValueTask HandleAsync(ConsumerPipelineContext context, ConsumerBehaviorHandler next, CancellationToken cancellationToken)
    {
        context.TransactionManager.Committing.AddHandler(_ => TrackEvent("Committing"));
        context.TransactionManager.Committed.AddHandler(_ => TrackEvent("Committed"));
        context.TransactionManager.Aborting.AddHandler(_ => TrackEvent("Aborting"));
        context.TransactionManager.Aborted.AddHandler(_ => TrackEvent("Aborted"));

        if (_isFailingAfterConsumerCommit)
            context.TransactionManager.Committed.AddHandler(_ => FailCommitIfNeeded());
        else
            context.TransactionManager.Committing.AddHandler(_ => FailCommitIfNeeded());

        return next(context, cancellationToken);
    }

    private ValueTask FailCommitIfNeeded() =>
        Interlocked.Decrement(ref _remainingFailingCommitsCount) >= 0
            ? ValueTask.FromException(new InvalidOperationException("Commit failed."))
            : ValueTask.CompletedTask;

    private ValueTask TrackEvent(string transactionEvent)
    {
        lock (_events)
        {
            _events.Add(transactionEvent);
        }

        return ValueTask.CompletedTask;
    }
}
