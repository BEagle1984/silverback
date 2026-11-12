// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Represent a stream of messages being published via the message bus. It is an enumerable that is asynchronously pushed with messages.
/// </summary>
internal interface IMessageStreamEnumerable
{
    /// <summary>
    ///     Adds the specified message to the stream. The returned <see cref="ValueTask" /> will complete only when the message has actually
    ///     been pulled and processed.
    /// </summary>
    /// <param name="message">
    ///     The message to be added.
    /// </param>
    /// <param name="onPullAction">
    ///     An action to be executed when the message is pulled.
    /// </param>
    /// <param name="onPullActionArgument">
    ///     The argument to be passed to the <paramref name="onPullAction" />.
    /// </param>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The <see cref="Task" /> will complete
    ///     only when the message has actually been pulled and processed. Its result is <c>false</c> if the stream was detached (see
    ///     <see cref="Detach" />) and the message must be pushed to the stream replacing it instead.
    /// </returns>
    Task<bool> PushAsync(object message, Action<object?>? onPullAction, object? onPullActionArgument, CancellationToken cancellationToken);

    /// <summary>
    ///     Aborts the ongoing enumeration and the pending calls to <see cref="PushAsync" />, then marks the stream as complete. Calling this
    ///     method will cause an <see cref="OperationCanceledException" /> to be thrown by the enumerator and the <see cref="PushAsync" /> method.
    /// </summary>
    void Abort();

    /// <summary>
    ///     Detaches the stream, whose subscriber returned and is invoked again with a new stream replacing it, while the other streams of
    ///     the same provider are still being enumerated.
    /// </summary>
    /// <remarks>
    ///     The pending push is acknowledged if the subscriber received its message, or if it's the first message pushed to the stream (the
    ///     subscriber was invoked for it). Otherwise it's refused, returning <c>false</c>, so that the message is pushed to the stream
    ///     replacing this one. The later pushes are refused as well, unless they push the first message.
    /// </remarks>
    void Detach();

    /// <summary>
    ///     Marks the stream as complete, meaning no more messages will be pushed.
    /// </summary>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task CompleteAsync(CancellationToken cancellationToken = default);
}
