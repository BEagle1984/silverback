// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Silverback.Background;

namespace Silverback.Messaging.LargeMessages
{
    public class ChunkStoreCleanerService : RecurringDistributedBackgroundService
    {
        private readonly IChunkStoreCleaner _storeCleaner;

        public ChunkStoreCleanerService(
            TimeSpan interval,
            IChunkStoreCleaner storeCleaner,
            DistributedLockSettings distributedLockSettings,
            IDistributedLockManager distributedLockManager,
            ILogger<ChunkStoreCleanerService> logger)
            : base(interval, distributedLockSettings, distributedLockManager, logger)
        {
            _storeCleaner = storeCleaner;
        }

        protected override Task ExecuteRecurringAsync(CancellationToken stoppingToken) =>
            _storeCleaner.Cleanup();
    }
}