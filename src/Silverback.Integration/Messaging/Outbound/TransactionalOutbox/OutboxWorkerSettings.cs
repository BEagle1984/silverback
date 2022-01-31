// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Lock;

namespace Silverback.Messaging.Outbound.TransactionalOutbox;

/// <summary>
///     The <see cref="OutboxWorker" /> and <see cref="OutboxWorkerService" /> settings.
/// </summary>
public record OutboxWorkerSettings(OutboxSettings Outbox)
{
    /// <summary>
    ///     Gets the settings for the optional <see cref="IDistributedLock" /> to be used to ensure that only one instance is running at
    ///     the same time. The default is <c>null</c> and no locking will be setup unless explicitly configured.
    /// </summary>
    public DistributedLockSettings? DistributedLock { get; init; }

    /// <summary>
    ///     Gets the interval between each run. The default is 500 milliseconds.
    /// </summary>
    public TimeSpan Interval { get; init; } = TimeSpan.FromMilliseconds(500);

    /// <summary>
    ///     Gets a value indicating whether the message order should be preserved, meaning that a failure in the produce of a message will
    ///     block the whole outbox. The default is <c>true</c>.
    /// </summary>
    public bool EnforceMessageOrder { get; init; } = true;

    /// <summary>
    ///     Gets the number of messages to be retrieved from the outbox and processed at once. The default is 1000.
    /// </summary>
    public int BatchSize { get; init; } = 1000;
}
