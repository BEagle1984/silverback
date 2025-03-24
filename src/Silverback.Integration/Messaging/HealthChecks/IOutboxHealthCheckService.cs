// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;

namespace Silverback.Messaging.HealthChecks;

/// <summary>
///     Checks that the outbox is being processed at a sustainable pace.
/// </summary>
public interface IOutboxHealthCheckService
{
    /// <summary>
    ///     Checks the age of the messages stored in the transactional outbox and optionally the queue length.
    /// </summary>
    /// <param name="maxAge">
    ///     The maximum message age, the check will fail when a message exceeds this age.
    /// </param>
    /// <param name="maxQueueLength">
    ///     The maximum amount of messages in the queue. The default is null, meaning unrestricted.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains a
    ///     boolean value indicating whether the check is successful.
    /// </returns>
    Task<bool> CheckIsHealthyAsync(TimeSpan maxAge, int? maxQueueLength = null);
}
