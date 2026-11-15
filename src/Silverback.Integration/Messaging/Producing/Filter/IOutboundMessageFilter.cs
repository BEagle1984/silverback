// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Producing.Filter;

/// <summary>
///     Can be used to filter out messages that should not be produced.
/// </summary>
public interface IOutboundMessageFilter
{
    /// <summary>
    ///     Returns a value indicating whether the specified message should be produced.
    /// </summary>
    /// <remarks>
    ///     The filter might also be invoked while the caller synchronously waits for the message to be produced, for example when using
    ///     the synchronous <see cref="IProducer" /> or <see cref="IPublisher" /> methods or when publishing a batch. Use
    ///     <c>ConfigureAwait(false)</c> when awaiting and keep the asynchronous work short, to avoid deadlocks and blocked threads.
    /// </remarks>
    /// <param name="envelope">
    ///     The envelope containing the message to be filtered.
    /// </param>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The task result contains a value indicating whether
    ///     the message should be produced.
    /// </returns>
    ValueTask<bool> ShouldProduceAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken);
}
