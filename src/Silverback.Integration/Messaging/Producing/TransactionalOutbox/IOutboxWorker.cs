// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     Processes the outbox and produces the messages to the target message broker endpoint.
/// </summary>
public interface IOutboxWorker
{
    /// <summary>
    ///     Processes the outbox relaying the stored messages.
    /// </summary>
    /// <param name="stoppingToken">
    ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains a value indicating whether
    ///     the outbox contained at least 1 message, and it was successfully processed. The caller should ideally keep invoking this method
    ///     in a loop, until <c>false</c> is returned.
    /// </returns>
    Task<bool> ProcessOutboxAsync(CancellationToken stoppingToken);

    /// <summary>
    ///     Returns the total number of messages in the outbox.
    /// </summary>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the number of messages in the
    ///     outbox.
    /// </returns>
    Task<int> GetLengthAsync();
}
