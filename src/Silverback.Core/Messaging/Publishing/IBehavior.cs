// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Messaging.Publishing;

/// <summary>
///     Can be used to build a custom pipeline, plugging some functionality into the
///     <see cref="IPublisher" />.
/// </summary>
public interface IBehavior
{
    /// <summary>
    ///     Process, handles or transforms the messages being published via the message bus.
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" /> that is publishing the message.
    /// </param>
    /// <param name="message">
    ///     The message being published.
    /// </param>
    /// <param name="next">
    ///     The next behavior in the pipeline.
    /// </param>
    /// <param name="cancellationToken">
    ///     The <see cref="CancellationToken" /> that can be used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The task result contains the
    ///     result values (if any).
    /// </returns>
    ValueTask<IReadOnlyCollection<object?>> HandleAsync(IPublisher publisher, object message, MessageHandler next, CancellationToken cancellationToken);
}
