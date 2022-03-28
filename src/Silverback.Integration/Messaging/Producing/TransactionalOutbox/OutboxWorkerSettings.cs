// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Configuration;
using Silverback.Lock;
using Silverback.Util;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     The <see cref="OutboxWorker" /> and <see cref="OutboxWorkerService" /> settings.
/// </summary>
public record OutboxWorkerSettings : IValidatableSettings
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxWorkerSettings" /> class.
    /// </summary>
    /// <param name="outboxSettings">
    ///     The outbox settings.
    /// </param>
    public OutboxWorkerSettings(OutboxSettings outboxSettings)
    {
        Outbox = Check.NotNull(outboxSettings, nameof(outboxSettings));
        DistributedLock = outboxSettings.GetCompatibleLockSettings();

        if (DistributedLock == null)
        {
            throw new SilverbackConfigurationException(
                $"The distributed lock settings cannot be inferred from the {outboxSettings.GetType().Name} since no matching " +
                "distributed lock implementation exists. Please specify the distributed lock implementation or explicitly set it to null.");
        }
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxWorkerSettings" /> class.
    /// </summary>
    /// <param name="outboxSettings">
    ///     The outbox settings.
    /// </param>
    /// <param name="lockSettings">
    ///     The distributed lock settings.
    /// </param>
    public OutboxWorkerSettings(OutboxSettings outboxSettings, DistributedLockSettings? lockSettings)
    {
        Outbox = outboxSettings;
        DistributedLock = lockSettings;
    }

    /// <summary>
    ///     Gets the outbox settings.
    /// </summary>
    public OutboxSettings Outbox { get; }

    /// <summary>
    ///     Gets the settings for the optional <see cref="IDistributedLock" /> to be used to ensure that only one instance is running at
    ///     the same time. By default it will be automatically inferred from the <see cref="Outbox" /> settings.
    /// </summary>
    public DistributedLockSettings? DistributedLock { get; }

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

    /// <inheritdoc cref="IValidatableSettings.Validate" />
    public void Validate()
    {
        if (Interval < TimeSpan.Zero)
            throw new SilverbackConfigurationException("The interval must be greater or equal to 0.");

        if (BatchSize < 1)
            throw new SilverbackConfigurationException("The batch size must be greater or equal to 1.");
    }
}
