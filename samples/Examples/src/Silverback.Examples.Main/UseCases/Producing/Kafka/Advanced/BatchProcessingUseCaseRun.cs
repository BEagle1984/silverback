// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Threading.Tasks;
using Silverback.Examples.Common.Messages;
using Silverback.Examples.Main.Menu;
using Silverback.Messaging.Publishing;

namespace Silverback.Examples.Main.UseCases.Producing.Kafka.Advanced
{
    [SuppressMessage("ReSharper", "UnusedType.Global", Justification = "Invoked by test framework")]
    public class BatchProcessingUseCaseRun : IAsyncRunnable
    {
        private readonly IEventPublisher _publisher;

        public BatchProcessingUseCaseRun(IEventPublisher publisher)
        {
            _publisher = publisher;
        }

        public async Task Run()
        {
            for (int i = 0; i < 22; i++)
            {
                await _publisher.PublishAsync(
                    new SampleBatchProcessedEvent
                    {
                        Content = i + 1 + " -" + DateTime.Now.ToString("HH:mm:ss.fff", CultureInfo.InvariantCulture)
                    });
            }
        }
    }
}
