// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using Silverback.Background;
using Silverback.Diagnostics;

namespace Silverback.Messaging.Chunking
{
    /// <summary>
    ///     The <see cref="IHostedService" />  that trigger the <see cref="IChunkStoreCleaner" /> at regular
    ///     intervals.
    /// </summary>
    public class ChunkStoreCleanerService : RecurringDistributedBackgroundService
    {
        private readonly IChunkStoreCleaner _storeCleaner;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChunkStoreCleanerService" /> class.
        /// </summary>
        /// <param name="interval">
        ///     The interval between each execution.
        /// </param>
        /// <param name="storeCleaner">
        ///     The <see cref="IChunkStoreCleaner" /> implementation.
        /// </param>
        /// <param name="distributedLockSettings">
        ///     Customizes the lock mechanism settings.
        /// </param>
        /// <param name="distributedLockManager">
        ///     The <see cref="IDistributedLockManager" />.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackLogger" />.
        /// </param>
        public ChunkStoreCleanerService(
            TimeSpan interval,
            IChunkStoreCleaner storeCleaner,
            DistributedLockSettings distributedLockSettings,
            IDistributedLockManager distributedLockManager,
            ISilverbackLogger<ChunkStoreCleanerService> logger)
            : base(interval, distributedLockSettings, distributedLockManager, logger)
        {
            _storeCleaner = storeCleaner;
        }

        /// <inheritdoc cref="RecurringDistributedBackgroundService.ExecuteRecurringAsync" />
        protected override Task ExecuteRecurringAsync(CancellationToken stoppingToken) =>
            _storeCleaner.Cleanup();
    }
}
