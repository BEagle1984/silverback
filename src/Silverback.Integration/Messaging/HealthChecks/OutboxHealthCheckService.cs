// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Silverback.Messaging.Outbound.TransactionalOutbox.Repositories;

namespace Silverback.Messaging.HealthChecks;

/// <inheritdoc cref="IOutboxHealthCheckService" />
public class OutboxHealthCheckService : IOutboxHealthCheckService
{
    private static readonly TimeSpan DefaultMaxAge = TimeSpan.FromSeconds(30);

    private readonly IOutboxReader _queueReader;

    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxHealthCheckService" /> class.
    /// </summary>
    /// <param name="queueReader">
    ///     The <see cref="IOutboxReader" />.
    /// </param>
    public OutboxHealthCheckService(IOutboxReader queueReader)
    {
        _queueReader = queueReader;
    }

    /// <inheritdoc cref="IOutboxHealthCheckService.CheckIsHealthyAsync" />
    public async Task<bool> CheckIsHealthyAsync(TimeSpan? maxAge = null, int? maxQueueLength = null)
    {
        if (maxQueueLength != null &&
            await _queueReader.GetLengthAsync().ConfigureAwait(false) > maxQueueLength)
            return false;

        return await _queueReader.GetMaxAgeAsync().ConfigureAwait(false) <= (maxAge ?? DefaultMaxAge);
    }
}
