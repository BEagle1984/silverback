// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;

namespace Silverback.Messaging.LargeMessages
{
    /// <inheritdoc cref="IChunkStoreCleaner" />
    public class ChunkStoreCleaner : IChunkStoreCleaner
    {
        private readonly IServiceScopeFactory _serviceScopeFactory;

        private readonly ISilverbackLogger<ChunkStoreCleaner> _logger;

        private readonly TimeSpan _retention;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ChunkStoreCleaner" /> class.
        /// </summary>
        /// <param name="retention">
        ///     The retention time of the stored chunks. The chunks will be discarded after this time is elapsed.
        /// </param>
        /// <param name="serviceScopeFactory">
        ///     The <see cref="IServiceScopeFactory" /> used to resolve the scoped types.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackLogger" />.
        /// </param>
        public ChunkStoreCleaner(
            TimeSpan retention,
            IServiceScopeFactory serviceScopeFactory,
            ISilverbackLogger<ChunkStoreCleaner> logger)
        {
            _retention = retention;
            _serviceScopeFactory = serviceScopeFactory;
            _logger = logger;
        }

        /// <inheritdoc cref="IChunkStoreCleaner.Cleanup" />
        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        public async Task Cleanup()
        {
            try
            {
                using var scope = _serviceScopeFactory.CreateScope();

                await scope.ServiceProvider
                    .GetRequiredService<IChunkStore>()
                    .Cleanup(DateTime.UtcNow.Subtract(_retention))
                    .ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                _logger.LogError(IntegrationEventIds.ErrorCleaningChunkStore, "Failed to cleanup the chunk store.", ex);
            }
        }
    }
}
