// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Hosting;
using Silverback.Background;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Outbound.Routing;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>AddOutboxWorker</c> and related methods to the <see cref="IBrokerOptionsBuilder" />.
    /// </summary>
    public static class BrokerOptionsBuilderAddOutboxWorkerExtensions
    {
        /// <summary>
        ///     Adds an <see cref="OutboxWorker" /> to publish the messages stored in the outbox to the configured
        ///     broker.
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <param name="interval">
        ///     The interval between each run. The default is 500ms.
        /// </param>
        /// <param name="enforceMessageOrder">
        ///     If set to <c>true</c> the message order will be ensured, retrying the same message until it can be
        ///     successfully
        ///     produced.
        /// </param>
        /// <param name="readBatchSize">
        ///     The number of messages to be loaded from the queue at once.
        /// </param>
        /// <param name="distributedLockSettings">
        ///     The settings for the locking mechanism. The default settings will be used if not specified.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddOutboxWorker(
            this IBrokerOptionsBuilder brokerOptionsBuilder,
            TimeSpan? interval = null,
            bool enforceMessageOrder = true,
            int readBatchSize = 100,
            DistributedLockSettings? distributedLockSettings = null)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            distributedLockSettings ??= new DistributedLockSettings();
            distributedLockSettings.EnsureResourceNameIsSet("OutboxWorker");

            brokerOptionsBuilder.SilverbackBuilder.Services
                .AddSingleton<IOutboxWorker>(
                    serviceProvider => new OutboxWorker(
                        serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                        serviceProvider.GetRequiredService<IBrokerCollection>(),
                        serviceProvider.GetRequiredService<IOutboundRoutingConfiguration>(),
                        serviceProvider.GetRequiredService<IOutboundLogger<OutboxWorker>>(),
                        enforceMessageOrder,
                        readBatchSize))
                .AddSingleton<IHostedService>(
                    serviceProvider => new OutboxWorkerService(
                        interval ?? TimeSpan.FromMilliseconds(500),
                        serviceProvider.GetRequiredService<IOutboxWorker>(),
                        distributedLockSettings,
                        serviceProvider.GetService<IDistributedLockManager>() ?? new NullLockManager(),
                        serviceProvider.GetRequiredService<ISilverbackLogger<OutboxWorkerService>>()));

            return brokerOptionsBuilder;
        }
    }
}
