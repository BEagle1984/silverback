// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Sequences.Unbounded;

/// <summary>
///     This isn't a real sequence but it's used to handle the stream pushed with all messages not actually
///     belonging to a sequence.
/// </summary>
/// <remarks>
///     Each message is committed by its own pipeline, as soon as the subscribers pull the next one or, if they stop enumerating, once the
///     sequence is aborted. The <see cref="ISequence.Context" /> is the context of the message that started the sequence, which is
///     disposed by its own pipeline.
/// </remarks>
internal sealed class UnboundedSequence : Sequence
{
    // The lower bits of _addState count the pipelines adding a message (see BeginAdd), the flags tell who handles the error that aborted
    // the sequence. They share the field, so that the abort can claim the error only while no message is being added, atomically.
    private const int ErrorClaimedByPipelineFlag = 1 << 29;

    private const int ErrorClaimedByAbortFlag = 1 << 30;

    [SuppressMessage("Usage", "CA2213:Disposable fields should be disposed", Justification = "Disposed in DisposeContext, called by Dispose")]
    private readonly IDisposable? _serviceScopeLease;

    private int _addState;

    [SuppressMessage("", "CA2000", Justification = "Stream disposed in base class")]
    public UnboundedSequence(string sequenceId, ConsumerPipelineContext context)
        : base(sequenceId, context, false, trackIdentifiers: false)
    {
        // The subscribers are resolved from the scope of the message that starts the sequence and keep using it until the sequence
        // ends, while the pipeline of that message disposes its context as soon as it completes
        _serviceScopeLease = context.LeaseServiceScope();
    }

    /// <summary>
    ///     Gets a value indicating whether the error that aborted the sequence is handled by the abort, applying the error policies,
    ///     because no message was being added. The messages added afterwards fail with the <see cref="ISequence.AbortException" />
    ///     without applying the error policies again.
    /// </summary>
    public bool IsErrorHandledByAbort => (_addState & ErrorClaimedByAbortFlag) != 0;

    // The transaction of the message that started the sequence is usually committed already, or still handled by its own pipeline:
    // let the pipeline of the message being added apply the error policies and roll back, or the abort itself if no message is being
    // added (see TryClaimErrorForAbort), let the pipeline commit the message being added when the subscribers stop enumerating, and
    // let each pipeline roll back its own message when the consumer stops (rolling it back here as well would fail its later commit)
    private protected override bool CompletesTransactionOnAbort => false;

    // A sequence aborted by an error stays in the store until the error is handled, so that the messages consumed in the meantime fail
    // with that error instead of starting a new stream. The PublisherConsumerBehavior removes it.
    private protected override bool IsRemovedFromStoreOnAbort => AbortReason != SequenceAbortReason.Error;

    /// <summary>
    ///     Signals that a pipeline is about to add a message to the sequence. It must be followed by a call to <see cref="EndAdd" />.
    /// </summary>
    public void BeginAdd() => Interlocked.Increment(ref _addState);

    /// <summary>
    ///     Signals that the pipeline completed adding the message (successfully or not) and checks whether the message must fail with the
    ///     <see cref="ISequence.AbortException" />, because the sequence was aborted by an error or by the cancellation requested by the
    ///     consumer stop.
    /// </summary>
    /// <remarks>
    ///     The pipeline handles the error (applying the error policies) unless the abort claimed it already, because no message was being
    ///     added (see <see cref="IsErrorHandledByAbort" />).
    /// </remarks>
    /// <returns>
    ///     A value indicating whether the message must fail with the <see cref="ISequence.AbortException" />.
    /// </returns>
    public bool EndAdd()
    {
        // Claim the error before decrementing, while the abort leaves it to this pipeline
        bool isAbortedByError = TryClaimErrorForPipeline();

        Interlocked.Decrement(ref _addState);

        // Check again after decrementing (a full fence), since the abort might have begun in the meantime: the abort is switched to the
        // aborted state before claiming the error, so either this check sees it or the abort sees no pipeline adding a message
        if (isAbortedByError || TryClaimErrorForPipeline())
            return true;

        // The cancellation requested by the consumer stop is rethrown as well, to roll back the message without applying the error
        // policies. An error that aborts the sequence from now on is left to the abort instead.
        return IsAborted && AbortReason != SequenceAbortReason.Error && AbortException != null;
    }

    /// <summary>
    ///     Claims the error that aborted the sequence, to be handled by the abort itself, if no message is being added and the error
    ///     wasn't claimed by the pipeline of a message added in the meantime.
    /// </summary>
    /// <remarks>
    ///     Called once the abort completed.
    /// </remarks>
    /// <returns>
    ///     A value indicating whether the abort must handle the error.
    /// </returns>
    public bool TryClaimErrorForAbort() =>
        AbortReason == SequenceAbortReason.Error &&
        Interlocked.CompareExchange(ref _addState, ErrorClaimedByAbortFlag, 0) == 0;

    // The context is disposed by the pipeline of its message: just release the scope
    private protected override void DisposeContext() => _serviceScopeLease?.Dispose();

    // Sets the flag even if the abort claimed the error already, since the abort flag prevails anyway (see IsErrorHandledByAbort). Check
    // IsAborted first, since the state is switched after the reason and the exception are set.
    private bool TryClaimErrorForPipeline()
    {
        if (!IsAborted || AbortReason != SequenceAbortReason.Error)
            return false;

        Interlocked.Or(ref _addState, ErrorClaimedByPipelineFlag);
        return true;
    }
}
