// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Messaging.Outbound.Deferred
{
    /// <summary>
    ///     Processes the outbound queue and produces the messages to the target message broker endpoint.
    /// </summary>
    public interface IOutboundQueueWorker
    {
        /// <summary>
        ///     Processes the outbound queue.
        /// </summary>
        /// <param name="stoppingToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> that represents the long running operations.
        /// </returns>
        Task ProcessQueue(CancellationToken stoppingToken);
    }
}
