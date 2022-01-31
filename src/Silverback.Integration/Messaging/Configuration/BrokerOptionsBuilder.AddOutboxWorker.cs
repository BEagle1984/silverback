// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

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
    // TODO: Builder version

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
