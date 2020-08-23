// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;
using Silverback.Examples.Common.Data;
using Silverback.Examples.Common.Messages;
using Silverback.Examples.Main.Menu;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Deferred
{
    [SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Invoked by test framework")]
    public class DeferredOutboundUseCaseRun : IAsyncRunnable
    {
        private readonly IEventPublisher _publisher;

        private readonly ExamplesDbContext _dbContext;

        public DeferredOutboundUseCaseRun(IEventPublisher publisher, ExamplesDbContext dbContext)
        {
            _publisher = publisher;
            _dbContext = dbContext;
        }

        public async Task Run()
        {
            await _publisher.PublishAsync(
                new SimpleIntegrationEvent
                {
                    Content = DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)
                });

            await _dbContext.SaveChangesAsync();
        }
    }
}
