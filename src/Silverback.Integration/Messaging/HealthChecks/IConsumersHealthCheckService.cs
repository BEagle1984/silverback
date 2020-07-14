// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Messaging.HealthChecks
{
    /// <summary>
    ///     Checks that all the consumers are connected.
    /// </summary>
    public interface IConsumersHealthCheckService
    {
        /// <summary>
        ///     Checks the status of all the consumers and reports as unhealthy if a consumer is in disconnected status.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation. The task result contains a boolean
        ///     value indicating whether the check is successful.
        /// </returns>
        Task<bool> CheckConsumersConnected();
    }
}
