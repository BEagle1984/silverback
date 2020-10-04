// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Hosting;
using Silverback.Background;
using Silverback.Diagnostics;
using Silverback.Messaging.Chunking;
using Silverback.Util;

// TODO: DELETE?

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    /// <summary>
    ///     Adds the <c>AddChunkStore</c> and related methods to the <see cref="IBrokerOptionsBuilder" />.
    /// </summary>
    public static class BrokerOptionsBuilderAddChunkStoreExtensions
    {
        /// <summary>
        ///     Adds a chunk store to temporary save the message chunks until the full message has been received.
        /// </summary>
        /// <typeparam name="TStore">
        ///     The type of the <see cref="IChunkStore" /> implementation to add.
        /// </typeparam>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <param name="retention">
        ///     The retention time of the stored chunks. The chunks will be discarded after this time is elapsed
        ///     (default is 1 hour).
        /// </param>
        /// <param name="cleanupInterval">
        ///     The interval between each cleanup (default is 10 minutes).
        /// </param>
        /// <param name="distributedLockSettings">
        ///     The settings for the locking mechanism (default settings will be used if not specified).
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddChunkStore<TStore>(
            this IBrokerOptionsBuilder brokerOptionsBuilder,
            TimeSpan? retention = null,
            TimeSpan? cleanupInterval = null,
            DistributedLockSettings? distributedLockSettings = null)
            where TStore : class, IChunkStore
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            distributedLockSettings ??= new DistributedLockSettings();
            distributedLockSettings.EnsureResourceNameIsSet("ChunkStoreCleaner");

            brokerOptionsBuilder.SilverbackBuilder.Services
                .AddScoped<IChunkStore, TStore>()
                //.AddScoped<ChunkAggregator>()
                .AddSingleton(
                    serviceProvider => new ChunkStoreCleaner(
                        retention ?? TimeSpan.FromDays(1),
                        serviceProvider.GetRequiredService<IServiceScopeFactory>(),
                        serviceProvider.GetRequiredService<ISilverbackLogger<ChunkStoreCleaner>>()))
                .AddSingleton<IHostedService>(
                    serviceProvider => new ChunkStoreCleanerService(
                        cleanupInterval ?? TimeSpan.FromMinutes(10),
                        serviceProvider.GetRequiredService<ChunkStoreCleaner>(),
                        distributedLockSettings,
                        serviceProvider.GetService<IDistributedLockManager>() ?? new NullLockManager(),
                        serviceProvider.GetRequiredService<ISilverbackLogger<ChunkStoreCleanerService>>()));

            return brokerOptionsBuilder;
        }

        /// <summary>
        ///     Adds a chunk store to temporary save the message chunks until the full message has been received.
        ///     This implementation stores the message chunks in memory.
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <param name="retention">
        ///     The retention time of the stored chunks. The chunks will be discarded after this time is elapsed
        ///     (default is 1 hour).
        /// </param>
        /// <param name="cleanupInterval">
        ///     The interval between each cleanup (default is 10 minutes).
        /// </param>
        /// <param name="distributedLockSettings">
        ///     The settings for the locking mechanism (default settings will be used if not specified).
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        public static IBrokerOptionsBuilder AddDbChunkStore(
            this IBrokerOptionsBuilder brokerOptionsBuilder,
            TimeSpan? retention = null,
            TimeSpan? cleanupInterval = null,
            DistributedLockSettings? distributedLockSettings = null)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            return brokerOptionsBuilder.AddChunkStore<DbChunkStore>(
                retention,
                cleanupInterval,
                distributedLockSettings);
        }

        /// <summary>
        ///     Adds a chunk store to temporary save the message chunks until the full message has been received.
        ///     This implementation stores the message chunks in the database.
        /// </summary>
        /// <param name="brokerOptionsBuilder">
        ///     The <see cref="IBrokerOptionsBuilder" /> that references the <see cref="IServiceCollection" /> to
        ///     add the services to.
        /// </param>
        /// <param name="retention">
        ///     The retention time of the stored chunks. The chunks will be discarded after this time is elapsed
        ///     (default is 1 hour).
        /// </param>
        /// <param name="cleanupInterval">
        ///     The interval between each cleanup (default is 10 minutes).
        /// </param>
        /// <returns>
        ///     The <see cref="IBrokerOptionsBuilder" /> so that additional calls can be chained.
        /// </returns>
        // TODO: DELETE?
        public static IBrokerOptionsBuilder AddInMemoryChunkStore(
            this IBrokerOptionsBuilder brokerOptionsBuilder,
            TimeSpan? retention = null,
            TimeSpan? cleanupInterval = null)
        {
            Check.NotNull(brokerOptionsBuilder, nameof(brokerOptionsBuilder));

            return brokerOptionsBuilder.AddChunkStore<InMemoryChunkStore>(
                retention,
                cleanupInterval,
                DistributedLockSettings.NoLock);
        }
    }
}
