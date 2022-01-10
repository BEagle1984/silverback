// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Lock;

namespace Silverback.Messaging.Outbound.TransactionalOutbox;

/// <summary>
///     The <see cref="OutboxWorker"/> and <see cref="OutboxWorkerService"/> settings.
/// </summary>
public record OutboxWorkerSettings
{
    public OutboxWorkerSettings(OutboxSettings outbox)
    {
        Outbox = outbox;
    }

    public OutboxSettings Outbox { get; init; }

    public DistributedLockSettings? DistributedLock { get; init; }

    public TimeSpan Interval { get; init; } = TimeSpan.FromMilliseconds(500);

    public bool EnforceMessageOrder { get; init; } = true;

    public int BatchSize { get; init; } = 1000;
}
