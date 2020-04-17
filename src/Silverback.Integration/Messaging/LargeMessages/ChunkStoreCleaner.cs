// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Silverback.Messaging.LargeMessages
{
    public class ChunkStoreCleaner : IChunkStoreCleaner
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;
        private readonly ILogger<ChunkStoreCleaner> _logger;
        private readonly TimeSpan _retention;

        public ChunkStoreCleaner(TimeSpan retention, IServiceScopeFactory serviceScopeFactory, ILogger<ChunkStoreCleaner> logger)
        {
            _retention = retention;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        public async Task Cleanup()
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();
                
                await scope.ServiceProvider
                    .GetRequiredService<IChunkStore>()
                    .Cleanup(DateTime.UtcNow.Subtract(_retention));
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to cleanup the chunk store.", ex);
            }
        }
    }
}