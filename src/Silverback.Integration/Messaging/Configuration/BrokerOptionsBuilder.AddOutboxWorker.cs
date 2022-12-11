// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Diagnostics;
using Silverback.Lock;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Implements the <c>AddOutboxWorker</c> methods.
/// </content>
public sealed partial class BrokerOptionsBuilder
{
    /// <summary>
    ///     Adds an <see cref="OutboxWorker" /> to publish the messages stored in the outbox to the configured broker.
    /// </summary>
    /// <param name="settingsBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="OutboxWorkerSettingsBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public BrokerOptionsBuilder AddOutboxWorker(Action<OutboxWorkerSettingsBuilder> settingsBuilderAction)
    {
        Check.NotNull(settingsBuilderAction, nameof(settingsBuilderAction));

        OutboxWorkerSettingsBuilder builder = new();
        settingsBuilderAction.Invoke(builder);

        return AddOutboxWorker(builder.Build());
    }

    /// <summary>
    ///     Adds an <see cref="OutboxWorker" /> to publish the messages stored in the outbox to the configured broker.
    /// </summary>
    /// <param name="settings">
    ///     The worker settings.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    public BrokerOptionsBuilder AddOutboxWorker(OutboxWorkerSettings settings)
    {
        Check.NotNull(settings, nameof(settings));

        SilverbackBuilder.Services
            .AddSingleton<IHostedService>(
                serviceProvider =>
                {
                    OutboxWorker outboxWorker = new(
                        settings,
                        serviceProvider.GetRequiredService<OutboxReaderFactory>().GetReader(settings.Outbox),
                        serviceProvider.GetRequiredService<IProducerCollection>(),
                        serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                        serviceProvider.GetRequiredService<IProducerLogger<OutboxWorker>>());

                    IDistributedLockFactory distributedLockFactory = serviceProvider.GetRequiredService<IDistributedLockFactory>();
                    IDistributedLock distributedLock = distributedLockFactory.GetDistributedLock(settings.DistributedLock);

                    return new OutboxWorkerService(
                        settings.Interval,
                        outboxWorker,
                        distributedLock,
                        serviceProvider.GetRequiredService<ISilverbackLogger<OutboxWorkerService>>());
                });

        return this;
    }
}
