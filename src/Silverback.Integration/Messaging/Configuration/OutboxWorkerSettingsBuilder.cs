// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Configuration;
using Silverback.Lock;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="OutboxWorkerSettings" />.
/// </summary>
public class OutboxWorkerSettingsBuilder
{
    private OutboxSettings? _outboxSettings;

    private TimeSpan? _interval;

    private bool? _enforceMessageOrder;

    private int? _batchSize;

    private Func<DistributedLockSettings?>? _lockSettingsProvider;

    /// <summary>
    ///     Specifies the <see cref="OutboxSettings" /> to be used by the outbox worker.
    /// </summary>
    /// <param name="outboxSettingsBuilderFunc">
    ///     A <see cref="Func{T}" /> that takes the <see cref="OutboxSettingsBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="OutboxWorkerSettingsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public OutboxWorkerSettingsBuilder ProcessOutbox(Func<OutboxSettingsBuilder, IOutboxSettingsImplementationBuilder> outboxSettingsBuilderFunc)
    {
        Check.NotNull(outboxSettingsBuilderFunc, nameof(outboxSettingsBuilderFunc));

        _outboxSettings = outboxSettingsBuilderFunc.Invoke(new OutboxSettingsBuilder()).Build();
        return this;
    }

    /// <summary>
    ///     Sets the interval between the outbox worker executions. The default is 500 milliseconds.
    /// </summary>
    /// <param name="interval">
    ///     The interval between the outbox worker executions.
    /// </param>
    /// <returns>
    ///     The <see cref="OutboxWorkerSettingsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public OutboxWorkerSettingsBuilder WithInterval(TimeSpan interval)
    {
        _interval = Check.Range(interval, nameof(interval), TimeSpan.Zero, TimeSpan.MaxValue);
        return this;
    }

    /// <summary>
    ///     Preserve the message ordering, meaning that a failure in the produce of a message will block the whole outbox.
    ///     This is the default.
    /// </summary>
    /// <returns>
    ///     The <see cref="OutboxWorkerSettingsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public OutboxWorkerSettingsBuilder EnforceMessageOrder()
    {
        _enforceMessageOrder = true;
        return this;
    }

    /// <summary>
    ///     Don't strictly enforce the message ordering, meaning that a failure in the produce of a message will cause it to be temporary
    ///     skipped to try to produce the next message in the queue.
    /// </summary>
    /// <returns>
    ///     The <see cref="OutboxWorkerSettingsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public OutboxWorkerSettingsBuilder DisableMessageOrderEnforcement()
    {
        _enforceMessageOrder = false;
        return this;
    }

    /// <summary>
    ///     Sets the number of messages to be retrieved from the outbox and processed at once. The default is 1000.
    /// </summary>
    /// <param name="batchSize">
    ///     The number of messages to be processed at once.
    /// </param>
    /// <returns>
    ///     The <see cref="OutboxWorkerSettingsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public OutboxWorkerSettingsBuilder WithBatchSize(int batchSize)
    {
        _batchSize = Check.Range(batchSize, nameof(batchSize), 1, int.MaxValue);
        return this;
    }

    /// <summary>
    ///     Explicitly configure the settings for the optional <see cref="IDistributedLock" /> to be used to ensure that only one instance
    ///     is running at the same time. By default it will be automatically inferred from the outbox settings.
    /// </summary>
    /// <param name="lockSettingsBuilderFunc">
    ///     A <see cref="Func{T}" /> that takes the <see cref="DistributedLockSettingsBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="OutboxWorkerSettingsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public OutboxWorkerSettingsBuilder WithDistributedLock(Func<DistributedLockSettingsBuilder, IDistributedLockSettingsImplementationBuilder> lockSettingsBuilderFunc)
    {
        Check.NotNull(lockSettingsBuilderFunc, nameof(lockSettingsBuilderFunc));

        _lockSettingsProvider = () => lockSettingsBuilderFunc.Invoke(new DistributedLockSettingsBuilder()).Build();
        return this;
    }

    /// <summary>
    ///     Explicitly disable the distributed lock.
    /// </summary>
    /// <returns>
    ///     The <see cref="OutboxWorkerSettingsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public OutboxWorkerSettingsBuilder WithoutDistributedLock()
    {
        _lockSettingsProvider = () => null;
        return this;
    }

    /// <summary>
    ///     Builds <see cref="OutboxWorkerSettings" />.
    /// </summary>
    /// <returns>
    ///     The <see cref="OutboxWorkerSettings" />.
    /// </returns>
    public OutboxWorkerSettings Build()
    {
        if (_outboxSettings == null)
            throw new SilverbackConfigurationException("The outbox settings are required. Use the ProcessOutbox method to set them.");

        OutboxWorkerSettings workerSettings = _lockSettingsProvider == null
            ? new OutboxWorkerSettings(_outboxSettings)
            : new OutboxWorkerSettings(_outboxSettings, _lockSettingsProvider.Invoke());

        if (_interval.HasValue)
            workerSettings = workerSettings with { Interval = _interval.Value };

        if (_enforceMessageOrder.HasValue)
            workerSettings = workerSettings with { EnforceMessageOrder = _enforceMessageOrder.Value };

        if (_batchSize.HasValue)
            workerSettings = workerSettings with { BatchSize = _batchSize.Value };

        workerSettings.Validate();

        return workerSettings;
    }
}
