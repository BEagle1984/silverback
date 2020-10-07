// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Messaging.Outbound.TransactionalOutbox
{
    /// <summary>
    ///     Processes the outbox and produces the messages to the target message broker endpoint.
    /// </summary>
    public interface IOutboxWorker
    {
        /// <summary>
        ///     Processes the outbox.
        /// </summary>
        /// <param name="stoppingToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> that represents the long running operations.
        /// </returns>
        Task ProcessQueueAsync(CancellationToken stoppingToken);
    }
}
