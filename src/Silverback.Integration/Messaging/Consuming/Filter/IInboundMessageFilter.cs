// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Consuming.Filter;

/// <summary>
///     Can be used to filter out the consumed messages that should not be processed.
/// </summary>
public interface IInboundMessageFilter
{
    /// <summary>
    ///     Returns a value indicating whether the specified consumed message should be processed.
    /// </summary>
    /// <remarks>
    ///     The messages that should not be processed are skipped: they aren't delivered to any subscriber and are committed (or
    ///     acknowledged) like the messages that no subscriber handles. An exception thrown by the filter is handled by the error policy
    ///     of the endpoint, like any other processing error.
    /// </remarks>
    /// <param name="envelope">
    ///     The envelope containing the deserialized message to be filtered.
    /// </param>
    /// <param name="cancellationToken">
    ///     A <see cref="CancellationToken" /> used to cancel the operation.
    /// </param>
    /// <returns>
    ///     A <see cref="ValueTask{TResult}" /> representing the asynchronous operation. The task result contains a value indicating whether
    ///     the message should be processed.
    /// </returns>
    ValueTask<bool> ShouldProcessAsync(IInboundEnvelope envelope, CancellationToken cancellationToken);
}
