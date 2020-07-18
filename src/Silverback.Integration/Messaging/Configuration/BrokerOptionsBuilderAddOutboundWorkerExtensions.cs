// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Hosting;
using Silverback.Background;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Connectors;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Util;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>AddOutboundWorker</c> and related methods to the <see cref="IBrokerOptionsBuilder" />.
    /// </summary>
    public static class BrokerOptionsBuilderAddOutboundWorkerExtensions
    {
        /// <summary>
        ///     Adds an <see cref="OutboundQueueWorker" /> to publish the queued messages to the configured broker.
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <param name="interval">
        ///     The interval between each run (default is 500ms).
        /// </param>
        /// <param name="enforceMessageOrder">
        ///     Specifies whether the messages must be produced in the same order as they were added to the queue.
        ///     If set to <c>true</c> the message order will be ensured, retrying the same message until it can be
        ///     successfully
        ///     produced.
        /// </param>
        /// <param name="readPackageSize">
        ///     The number of messages to be loaded from the queue at once.
        /// </param>
        /// <param name="distributedLockSettings">
        ///     The settings for the locking mechanism (default settings will be used if not specified).
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddOutboundWorker(
            this IBrokerOptionsBuilder brokerOptionsBuilder,
            TimeSpan? interval = null,
            bool enforceMessageOrder = true,
            int readPackageSize = 100,
            DistributedLockSettings? distributedLockSettings = null)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            distributedLockSettings ??= new DistributedLockSettings();
            distributedLockSettings.EnsureResourceNameIsSet("OutboundQueueWorker");

            brokerOptionsBuilder.SilverbackBuilder.Services
                .AddSingleton<IOutboundQueueWorker>(
                    serviceProvider => new OutboundQueueWorker(
                        serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                        serviceProvider.GetRequiredService<IBrokerCollection>(),
                        serviceProvider.GetRequiredService<IOutboundRoutingConfiguration>(),
                        serviceProvider.GetRequiredService<ISilverbackLogger<OutboundQueueWorker>>(),
                        enforceMessageOrder,
                        readPackageSize))
                .AddSingleton<IHostedService>(
                    serviceProvider => new OutboundQueueWorkerService(
                        interval ?? TimeSpan.FromMilliseconds(500),
                        serviceProvider.GetRequiredService<IOutboundQueueWorker>(),
                        distributedLockSettings,
                        serviceProvider.GetService<IDistributedLockManager>() ?? new NullLockManager(),
                        serviceProvider.GetRequiredService<ISilverbackLogger<OutboundQueueWorkerService>>()));

            return brokerOptionsBuilder;
        }

        /// <summary>
        ///     Adds an <see cref="OutboundQueueWorker" /> to publish the queued messages to the configured broker.
        /// </summary>
        /// <typeparam name="TQueueReader">
        ///     The type of the <see cref="IOutboundQueueReader" /> to be used.
        /// </typeparam>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <param name="interval">
        ///     The interval between each run (default is 500ms).
        /// </param>
        /// <param name="enforceMessageOrder">
        ///     If set to <c>true</c> the message order will be ensured, retrying the same message until it can be
        ///     successfully
        ///     produced.
        /// </param>
        /// <param name="readPackageSize">
        ///     The number of messages to be loaded from the queue at once.
        /// </param>
        /// <param name="distributedLockSettings">
        ///     The settings for the locking mechanism (default settings will be used if not specified).
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddOutboundWorker<TQueueReader>(
            this IBrokerOptionsBuilder brokerOptionsBuilder,
            TimeSpan? interval = null,
            bool enforceMessageOrder = true,
            int readPackageSize = 100,
            DistributedLockSettings? distributedLockSettings = null)
            where TQueueReader : class, IOutboundQueueReader
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.AddOutboundWorker(
                interval,
                enforceMessageOrder,
                readPackageSize,
                distributedLockSettings);

            brokerOptionsBuilder.SilverbackBuilder.Services
                .AddScoped<IOutboundQueueReader, TQueueReader>();

            return brokerOptionsBuilder;
        }

        /// <summary>
        ///     Adds an <see cref="OutboundQueueWorker" /> to publish the queued messages to the configured broker.
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <param name="interval">
        ///     The interval between each run (default is 500ms).
        /// </param>
        /// <param name="enforceMessageOrder">
        ///     If set to <c>true</c> the message order will be ensured, retrying the same message until it can be
        ///     successfully
        ///     produced.
        /// </param>
        /// <param name="readPackageSize">
        ///     The number of messages to be loaded from the queue at once.
        /// </param>
        /// <param name="distributedLockSettings">
        ///     The settings for the locking mechanism (default settings will be used if not specified).
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddDbOutboundWorker(
            this IBrokerOptionsBuilder brokerOptionsBuilder,
            TimeSpan? interval = null,
            bool enforceMessageOrder = true,
            int readPackageSize = 100,
            DistributedLockSettings? distributedLockSettings = null)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            brokerOptionsBuilder.AddOutboundWorker<DbOutboundQueueReader>(
                interval,
                enforceMessageOrder,
                readPackageSize,
                distributedLockSettings);

            return brokerOptionsBuilder;
        }

        /// <summary>
        ///     Adds an <see cref="OutboundQueueWorker" /> to publish the queued messages to the configured broker.
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <param name="distributedLockSettings">
        ///     The settings for the locking mechanism.
        /// </param>
        /// <param name="interval">
        ///     The interval between each run (default is 500ms).
        /// </param>
        /// <param name="enforceMessageOrder">
        ///     if set to <c>true</c> the message order will be preserved (no message will be skipped).
        /// </param>
        /// <param name="readPackageSize">
        ///     The number of messages to be loaded from the queue at once.
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddDbOutboundWorker(
            this IBrokerOptionsBuilder brokerOptionsBuilder,
            DistributedLockSettings? distributedLockSettings,
            TimeSpan? interval = null,
            bool enforceMessageOrder = true,
            int readPackageSize = 100)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            return brokerOptionsBuilder.AddDbOutboundWorker(
                interval,
                enforceMessageOrder,
                readPackageSize,
                distributedLockSettings);
        }
    }
}
