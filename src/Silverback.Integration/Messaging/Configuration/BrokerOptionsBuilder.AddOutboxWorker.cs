// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Silverback.Diagnostics;
using Silverback.Lock;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <content>
///     Adds the AddOutboxWorker method to the <see cref="BrokerOptionsBuilder" />.
/// </content>
public sealed partial class BrokerOptionsBuilder
{
    /// <summary>
    ///     Adds an <see cref="OutboxWorker" /> to publish the messages stored in the outbox to the configured broker.
    /// </summary>
    /// <param name="interval">
    ///     The interval between each run. The default is 500ms.
    /// </param>
    /// <param name="enforceMessageOrder">
    ///     If set to <c>true</c> the message order will be ensured, retrying the same message until it can be successfully produced.
    /// </param>
    /// <param name="batchSize">
    ///     The number of messages to be loaded and processed at once.
    /// </param>
    /// <param name="configureLockFunction">
    ///     A <see cref="Func{T, TResult}" /> that takes the <see cref="IDistributedLockSettingsBuilder" /> and configures it, returning an
    ///     <see cref="IDistributedLockSettingsBuilder" /> that will in turn instantiate the actual <see cref="IDistributedLock" /> according to
    ///     the chosen implementation.
    /// </param>
    /// <returns>
    ///     The <see cref="BrokerOptionsBuilder" /> so that additional calls can be chained.
    /// </returns>
    // public BrokerOptionsBuilder AddOutboxWorker(
    //     TimeSpan? interval = null,
    //     bool enforceMessageOrder = true,
    //     int batchSize = 1000,
    //     Func<DistributedLockBuilder, IDistributedLockBuilder>? configureLockFunction = null) =>
    //     AddOutboxWorker(
    //         interval,
    //         enforceMessageOrder,
    //         batchSize,
    //         configureLockFunction?.Invoke(new DistributedLockBuilder()).Build());


    // TODO: OutboxWorkerSettings and Builder

    /// <summary>
    ///     Adds an <see cref="OutboxWorker" /> to publish the messages stored in the outbox to the configured broker.
    /// </summary>
    /// <param name="interval">
    ///     The interval between each run. The default is 500ms.
    /// </param>
    /// <param name="enforceMessageOrder">
    ///     If set to <c>true</c> the message order will be ensured, retrying the same message until it can be successfully produced.
    /// </param>
    /// <param name="batchSize">
    ///     The number of messages to be loaded and processed at once.
    /// </param>
    /// <param name="distributedLockSettings">
    ///     The settings of the distributed lock to be used to ensure that one and only one instance of the worker is running at any time.
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
                    OutboxWorker outboxWorker = new OutboxWorker(
                        settings,
                        serviceProvider.GetRequiredService<OutboxReaderFactory>().GetReader(settings.Outbox),
                        serviceProvider.GetRequiredService<IBrokerCollection>(),
                        serviceProvider.GetRequiredService<IOutboundRoutingConfiguration>(),
                        serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                        serviceProvider.GetRequiredService<IOutboundLogger<OutboxWorker>>());

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
