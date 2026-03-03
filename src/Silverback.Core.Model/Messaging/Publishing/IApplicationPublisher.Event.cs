// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing;

/// <content>
///     Defines the <c>PublishEvent</c>/<c>PublishEventAsync</c> methods.
/// </content>
public partial interface IApplicationPublisher
{
    /// <summary>
    ///     Publishes the specified event to its subscribers via the message bus and the method will not
    ///     complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume the message
    ///     through a message broker).
    /// </summary>
    /// <param name="eventMessage">
    ///     The event to be published.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    void PublishEvent(IEvent eventMessage, bool throwIfUnhandled = false);

    /// <summary>
    ///     Publishes the specified event to its subscribers via the message bus and the <see cref="Task" />
    ///     will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume the message
    ///     through a message broker).
    /// </summary>
    /// <param name="eventMessage">
    ///     The event to be published.
    /// </param>
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task PublishEventAsync(IEvent eventMessage, CancellationToken cancellationToken = default);

    /// <summary>
    ///     Publishes the specified event to its subscribers via the message bus and the <see cref="Task" />
    ///     will not complete until all subscribers have processed it (unless using Silverback.Integration to produce and consume the message
    ///     through a message broker).
    /// </summary>
    /// <param name="eventMessage">
    ///     The event to be published.
    /// </param>
    /// <param name="throwIfUnhandled">
    ///     A boolean value indicating whether an exception must be thrown if no subscriber is handling the message.
    /// </param>
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="Task" /> representing the asynchronous operation.
    /// </returns>
    Task PublishEventAsync(IEvent eventMessage, bool throwIfUnhandled, CancellationToken cancellationToken = default);
}
