// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Messages;

/// <summary>
///     Represent a future <see cref="IMessageStreamEnumerable{TMessage}" />, that will created as soon
///     as the first message is pushed.
/// </summary>
internal interface ILazyMessageStreamEnumerable
{
    /// <summary>
    ///     Gets the type of the messages being streamed.
    /// </summary>
    Type MessageType { get; }

    /// <summary>
    ///     Gets the <see cref="IMessageStreamEnumerable{TMessage}" />, as soon as it is created.
    /// </summary>
    IMessageStreamEnumerable? Stream { get; }

    /// <summary>
    ///     Gets the filters to be applied.
    /// </summary>
    IReadOnlyCollection<IMessageFilter>? Filters { get; }

    /// <summary>
    ///     Gets a value indicating whether the subscriber consuming the stream returned without throwing.
    /// </summary>
    bool HasSubscriberReturned { get; }

    /// <summary>
    ///     Gets an awaitable <see cref="Task" /> that completes when the first message is pushed and the
    ///     <see cref="IMessageStreamEnumerable" /> is created. The created stream can be retrieved via
    ///     the <see cref="Stream" /> property.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation. The task result
    ///     contains the <see cref="IMessageStreamEnumerable" />.
    /// </returns>
    Task WaitUntilCreatedAsync();

    /// <summary>
    ///     Gets the already created <see cref="IMessageStreamEnumerable" /> or creates it on the fly.
    /// </summary>
    /// <returns>
    ///     The stream.
    /// </returns>
    IMessageStreamEnumerable GetOrCreateStream();

    /// <summary>
    ///     Gets the already created <see cref="IMessageStreamEnumerable" /> or creates it on the fly, without signaling its creation
    ///     (see <see cref="WaitUntilCreatedAsync" />) until <see cref="SignalCreated" /> is called.
    /// </summary>
    /// <param name="isCreated">
    ///     A value indicating whether the stream was created by this call.
    /// </param>
    /// <returns>
    ///     The stream.
    /// </returns>
    IMessageStreamEnumerable GetOrCreateStreamWithoutSignaling(out bool isCreated);

    /// <summary>
    ///     Signals the creation of the stream created by <see cref="GetOrCreateStreamWithoutSignaling" />, completing the
    ///     <see cref="WaitUntilCreatedAsync" /> task.
    /// </summary>
    void SignalCreated();

    /// <summary>
    ///     Aborts the stream or cancels its creation.
    /// </summary>
    void Abort();

    /// <summary>
    ///     Completes the stream or cancels its creation.
    /// </summary>
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> to monitor for cancellation requests.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task CompleteAsync(CancellationToken cancellationToken = default);
}
