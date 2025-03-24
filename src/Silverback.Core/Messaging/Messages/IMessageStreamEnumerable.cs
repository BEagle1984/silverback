// Copyright (c) 2025 Sergio Aquilini
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
    ///     A <see cref="Task" /> representing the asynchronous operation. The  <see cref="Task" /> will complete
    ///     only when the message has actually been pulled and processed.
    /// </returns>
    Task PushAsync(object message, Action<object?>? onPullAction, object? onPullActionArgument, CancellationToken cancellationToken);

    /// <summary>
    ///     Aborts the ongoing enumeration and the pending calls to <see cref="PushAsync" />, then marks the stream as complete. Calling this
    ///     method will cause an <see cref="OperationCanceledException" /> to be thrown by the enumerator and the <see cref="PushAsync" /> method.
    /// </summary>
    void Abort();

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
