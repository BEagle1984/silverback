// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.HealthChecks
{
    /// <summary>
    ///     Checks that all the consumers are connected.
    /// </summary>
    public interface IConsumersHealthCheckService
    {
        /// <summary>
        ///     Checks the status of all the consumers and returns a collection containing the consumer instances that
        ///     don't appear to be fully connected (Status >= <see cref="ConsumerStatus.Ready" />).
        /// </summary>
        /// <param name="minStatus">
        ///     The minimum <see cref="ConsumerStatus" /> a consumer must have to be considered fully connected.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
        ///     consumers that aren't fully connected.
        /// </returns>
        Task<IReadOnlyCollection<IConsumer>> GetDisconnectedConsumersAsync(ConsumerStatus minStatus);
    }
}
