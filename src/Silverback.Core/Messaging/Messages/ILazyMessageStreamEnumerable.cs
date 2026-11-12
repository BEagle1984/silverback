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
    ///     Gets the lazy stream replacing this one, if its subscriber returned and is invoked again with a new stream (see
    ///     <see cref="CreateReplacement" />).
    /// </summary>
    ILazyMessageStreamEnumerable? Replacement { get; }

    /// <summary>
    ///     Creates the lazy stream, with the same message type and filters, that replaces this one for the subscriber that returned.
    /// </summary>
    /// <returns>
    ///     The new <see cref="ILazyMessageStreamEnumerable" />, which is also set as <see cref="Replacement" />.
    /// </returns>
    ILazyMessageStreamEnumerable CreateReplacement();

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
