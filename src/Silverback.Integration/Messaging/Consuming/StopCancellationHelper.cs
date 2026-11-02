// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using Silverback.Messaging.Sequences;

namespace Silverback.Messaging.Consuming;

/// <summary>
///     Recognizes the cancellation of the processing requested by the consumer stop, which is not a processing failure: the consumer
///     cancels the token passed to the pipeline when it is stopped, and a subscriber honoring it throws an
///     <see cref="OperationCanceledException" />.
/// </summary>
internal static class StopCancellationHelper
{
    /// <summary>
    ///     Checks whether the exception is the cancellation requested by the consumer stop.
    /// </summary>
    /// <param name="exception">
    ///     The exception thrown by the processing.
    /// </param>
    /// <param name="cancellationToken">
    ///     The cancellation token passed to the consumer pipeline.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the processing was canceled by the consumer stop, otherwise <c>false</c>.
    /// </returns>
    public static bool IsStopCancellation(Exception exception, CancellationToken cancellationToken) =>
        exception is OperationCanceledException && cancellationToken.IsCancellationRequested;

    /// <summary>
    ///     Checks whether the exception is the cancellation requested by the consumer stop, according to the reason why the sequence
    ///     was aborted.
    /// </summary>
    /// <remarks>
    ///     The abort reason is determined when the exception is first caught (see <see cref="GetSequenceAbortReason" />). Checking the
    ///     token afterwards isn't reliable, since the error policies applied by the abort might have stopped the consumer in the
    ///     meantime, canceling the token.
    /// </remarks>
    /// <param name="exception">
    ///     The exception thrown by the processing.
    /// </param>
    /// <param name="abortedSequence">
    ///     The aborted <see cref="ISequence" /> that the message belongs or was pushed to.
    /// </param>
    /// <returns>
    ///     <c>true</c> if the processing was canceled by the consumer stop, otherwise <c>false</c>.
    /// </returns>
    public static bool IsStopCancellation(Exception exception, ISequence abortedSequence) =>
        exception is OperationCanceledException && abortedSequence.AbortReason == SequenceAbortReason.ConsumerAborted;

    /// <summary>
    ///     Gets the reason to abort the sequence whose processing threw: the cancellation requested by the consumer stop rolls the
    ///     sequence back, as when the consumer aborts it, instead of applying the error policies.
    /// </summary>
    /// <param name="exception">
    ///     The exception thrown by the processing.
    /// </param>
    /// <param name="cancellationToken">
    ///     The cancellation token passed to the consumer pipeline.
    /// </param>
    /// <returns>
    ///     The <see cref="SequenceAbortReason" />.
    /// </returns>
    public static SequenceAbortReason GetSequenceAbortReason(Exception exception, CancellationToken cancellationToken) =>
        IsStopCancellation(exception, cancellationToken) ? SequenceAbortReason.ConsumerAborted : SequenceAbortReason.Error;
}
