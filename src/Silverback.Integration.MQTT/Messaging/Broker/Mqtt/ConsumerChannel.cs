// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Diagnostics;

namespace Silverback.Messaging.Broker.Mqtt;

/// <summary>
///     The channel buffering the messages to be processed by one reader. Unlike Kafka, MQTT can't seek: the channel keeps the messages
///     that were read and not acknowledged yet (in the order they were received) and replays them from memory when they are rolled back.
/// </summary>
internal class ConsumerChannel : ConsumerChannel<ConsumedApplicationMessage>
{
    private readonly System.Threading.Lock _messagesLock = new();

    // The messages read and neither acknowledged nor discarded yet, in the order they were received
    private readonly LinkedList<ConsumedApplicationMessage> _pendingMessages = [];

    private ConsumedApplicationMessage? _currentMessage; // The message being processed by the reader

    private int _replayPendingCount; // The number of pending messages to be replayed

    private int _restartRequestsCount; // Set under the lock by the rollbacks, while the restart is pending the reader must stop

    public ConsumerChannel(int capacity, int index, ISilverbackLogger logger)
        : base(capacity, index.ToString(CultureInfo.InvariantCulture), logger)
    {
    }

    /// <summary>
    ///     The state of a message after it was processed by the pipeline.
    /// </summary>
    public enum PendingState
    {
        /// <summary>
        ///     The message was committed or rolled back (or its acknowledgment failed).
        /// </summary>
        Resolved,

        /// <summary>
        ///     The message is pending and tracked by the sequence it was added to, which commits or rolls it back.
        /// </summary>
        Tracked,

        /// <summary>
        ///     The message is pending but no sequence tracks it, so nothing will commit it.
        /// </summary>
        Untracked
    }

    /// <summary>
    ///     Gets a value indicating whether a rollback requested to restart the reader. The reader must stop processing until it's
    ///     restarted, not to process the messages to be replayed (or the later messages) in a sequence that the restart would abort.
    /// </summary>
    public bool IsRestartPending => Volatile.Read(ref _restartRequestsCount) > 0;

    // The replayed messages start their sequences again in the restarted reader, with the same ids (e.g., the chunk sequences)
    protected override bool IsSequenceStoreReplacedOnRestart => true;

    /// <summary>
    ///     Reads the next message to be processed: the oldest message to be replayed, if any, otherwise the next buffered message.
    /// </summary>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The task result contains the message.
    /// </returns>
    /// <exception cref="OperationCanceledException">
    ///     A restart of the reader is pending.
    /// </exception>
    public async ValueTask<ConsumedApplicationMessage> ReadNextAsync()
    {
        lock (_messagesLock)
        {
            ThrowIfRestartPendingCore();

            if (TryTakeReplayCore(out ConsumedApplicationMessage? replayMessage))
                return replayMessage;
        }

        ConsumedApplicationMessage message = await ReadAsync().ConfigureAwait(false);

        lock (_messagesLock)
        {
            message.PendingNode = _pendingMessages.AddLast(message);

            // A rollback may have requested a replay (or a restart) while waiting: the older messages must be processed first
            if (IsRestartPending)
            {
                SetReplayPendingCore(message);
                ThrowIfRestartPendingCore();
            }

            if (TryTakeReplayCore(out ConsumedApplicationMessage? replayMessage))
            {
                SetReplayPendingCore(message);
                return replayMessage;
            }

            _currentMessage = message;
            return message;
        }
    }

    /// <summary>
    ///     Called by the reader after the message was processed by the pipeline, to move on while the message is still pending, leaving
    ///     its commit or rollback to the sequence it was added to.
    /// </summary>
    /// <param name="message">
    ///     The message that was processed.
    /// </param>
    /// <param name="replayCount">
    ///     The number of times the message was replayed, which identifies the processing attempt.
    /// </param>
    /// <returns>
    ///     The <see cref="PendingState" /> of the message.
    /// </returns>
    public PendingState Defer(ConsumedApplicationMessage message, out int replayCount)
    {
        lock (_messagesLock)
        {
            replayCount = message.ReplayCount;

            if (message.TaskCompletionSource.Task.IsCompleted || message.IsReplayPending)
                return PendingState.Resolved;

            message.IsDeferred = true;
            return message.IsTracked ? PendingState.Tracked : PendingState.Untracked;
        }
    }

    /// <summary>
    ///     Returns a value indicating whether the message is still pending and untracked in the specified processing attempt, meaning
    ///     that it was neither committed, rolled back nor read again to be replayed.
    /// </summary>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="replayCount">
    ///     The replay count returned by <see cref="Defer" />.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the message is still pending and untracked, otherwise <c>false</c>.
    /// </returns>
    public bool IsStillUntracked(ConsumedApplicationMessage message, int replayCount)
    {
        lock (_messagesLock)
        {
            return IsPendingCore(message) && message.ReplayCount == replayCount && !message.IsReplayPending && !message.IsTracked &&
                   !message.TaskCompletionSource.Task.IsCompleted;
        }
    }

    /// <summary>
    ///     Marks the message as tracked by the sequence it was added to.
    /// </summary>
    /// <param name="message">
    ///     The tracked message.
    /// </param>
    public void MarkTracked(ConsumedApplicationMessage message)
    {
        lock (_messagesLock)
        {
            message.IsTracked = true;
        }
    }

    /// <summary>
    ///     Puts back a message taken by a stopping reader, so that it's processed again when the reader restarts, unless it was
    ///     committed or rolled back already.
    /// </summary>
    /// <param name="message">
    ///     The message to put back.
    /// </param>
    public void PutBack(ConsumedApplicationMessage message)
    {
        lock (_messagesLock)
        {
            if (IsPendingCore(message) && !message.TaskCompletionSource.Task.IsCompleted)
                SetReplayPendingCore(message);
        }
    }

    /// <summary>
    ///     Removes the message after it was acknowledged and completes its task.
    /// </summary>
    /// <param name="message">
    ///     The acknowledged message.
    /// </param>
    public void Acknowledged(ConsumedApplicationMessage message)
    {
        lock (_messagesLock)
        {
            RemoveCore(message);
            message.TaskCompletionSource.TrySetResult(true);
        }
    }

    /// <summary>
    ///     Faults the task of the message whose acknowledgment failed.
    /// </summary>
    /// <param name="message">
    ///     The message.
    /// </param>
    /// <param name="exception">
    ///     The exception.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the reader is still waiting for the pipeline to process the message and therefore observes the failure, or
    ///     <c>false</c> if the reader moved on.
    /// </returns>
    public bool FailAcknowledgment(ConsumedApplicationMessage message, Exception exception)
    {
        lock (_messagesLock)
        {
            message.TaskCompletionSource.TrySetException(exception);
            return _currentMessage == message && !message.IsDeferred;
        }
    }

    /// <summary>
    ///     Rolls back the specified messages, scheduling them to be replayed. Like a seek, the later messages are replayed as well, to
    ///     preserve the order.
    /// </summary>
    /// <param name="messages">
    ///     The messages to be rolled back.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the reader must be restarted and the caller must perform the restart, or <c>false</c> if nothing was rolled
    ///     back, the rolled back message is the one being processed by the reader (which reads it again right away), the reader is
    ///     stopping (and replays the messages when restarted) or a restart is pending already.
    /// </returns>
    public bool Rollback(IReadOnlyCollection<ConsumedApplicationMessage> messages)
    {
        lock (_messagesLock)
        {
            // The messages of a reset channel belong to the disconnected session
            ConsumedApplicationMessage[] currentMessages = [.. messages.Where(IsPendingCore)];

            if (currentMessages.Length == 0)
                return false;

            // The reader observes the rollback of the message it's processing and reads it again, as long as no later message was read.
            // A message tracked by a sequence is rolled back with its sequence (even before the reader moves on), which requires the
            // restart to abort the sequence and replay its other messages.
            bool isObservedByReader = currentMessages.Length == 1 && _currentMessage == currentMessages[0] &&
                                      !currentMessages[0].IsDeferred && !currentMessages[0].IsTracked &&
                                      _pendingMessages.Last?.Value == currentMessages[0];

            long fromPosition = currentMessages.Min(message => message.Position);

            for (LinkedListNode<ConsumedApplicationMessage>? node = _pendingMessages.Last;
                 node != null && node.Value.Position >= fromPosition;
                 node = node.Previous)
            {
                SetReplayPendingCore(node.Value);
                node.Value.TaskCompletionSource.TrySetResult(false);
            }

            if (isObservedByReader || IsReadCanceled)
                return false;

            // Requested under the lock, so that the reader can't take the messages to be replayed before stopping
            return Interlocked.Increment(ref _restartRequestsCount) == 1;
        }
    }

    /// <summary>
    ///     Gets the number of pending restart requests.
    /// </summary>
    /// <returns>
    ///     The number of pending requests.
    /// </returns>
    public int GetRestartRequestsCount() => Volatile.Read(ref _restartRequestsCount);

    /// <summary>
    ///     Marks the specified number of restart requests as handled.
    /// </summary>
    /// <param name="count">
    ///     The number of handled requests.
    /// </param>
    /// <returns>
    ///     <c>true</c> if further requests were added in the meantime, otherwise <c>false</c>.
    /// </returns>
    public bool RemoveRestartRequests(int count) => Interlocked.Add(ref _restartRequestsCount, -count) > 0;

    /// <summary>
    ///     Discards the pending restart requests, when the restart is overtaken by a stop. The messages to be replayed are kept and
    ///     processed when the reader is started again.
    /// </summary>
    public void ClearRestartRequests() => Interlocked.Exchange(ref _restartRequestsCount, 0);

    /// <inheritdoc cref="ConsumerChannel{T}.Reset" />
    public override void Reset()
    {
        base.Reset();

        // The pending messages belong to the disconnected session and are replayed by the broker
        lock (_messagesLock)
        {
            foreach (ConsumedApplicationMessage message in _pendingMessages)
            {
                message.PendingNode = null;
            }

            _pendingMessages.Clear();
            _currentMessage = null;
            _replayPendingCount = 0;
        }
    }

    private bool IsPendingCore(ConsumedApplicationMessage message) => message.PendingNode?.List == _pendingMessages;

    private void SetReplayPendingCore(ConsumedApplicationMessage message)
    {
        if (message.IsReplayPending)
            return;

        message.IsReplayPending = true;
        _replayPendingCount++;
    }

    private void RemoveCore(ConsumedApplicationMessage message)
    {
        if (!IsPendingCore(message))
            return;

        _pendingMessages.Remove(message.PendingNode!);
        message.PendingNode = null;

        if (message.IsReplayPending)
        {
            message.IsReplayPending = false;
            _replayPendingCount--;
        }

        if (_currentMessage == message)
            _currentMessage = null;
    }

    private void ThrowIfRestartPendingCore()
    {
        if (IsRestartPending)
            throw new OperationCanceledException("The reader is being restarted.");
    }

    private bool TryTakeReplayCore([NotNullWhen(true)] out ConsumedApplicationMessage? message)
    {
        message = null;

        if (_replayPendingCount == 0)
            return false;

        for (LinkedListNode<ConsumedApplicationMessage>? node = _pendingMessages.First; node != null; node = node.Next)
        {
            if (!node.Value.IsReplayPending)
                continue;

            message = node.Value;
            _replayPendingCount--;
            message.ResetForReplay();
            _currentMessage = message;
            return true;
        }

        return false;
    }
}
