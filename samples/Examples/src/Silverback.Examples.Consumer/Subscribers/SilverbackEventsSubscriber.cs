// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;

namespace Silverback.Examples.Consumer.Subscribers
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Subscriber")]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Subscriber")]
    public class SilverbackEventsSubscriber
    {
        private readonly ILogger<SilverbackEventsSubscriber> _logger;

        public SilverbackEventsSubscriber(ILogger<SilverbackEventsSubscriber> logger)
        {
            _logger = logger;
        }

        public void OnBatchComplete(BatchCompleteEvent message) =>
            _logger.LogInformation($"Batch '{message.BatchId} ready ({message.BatchSize} messages)");

        public void OnBatchProcessed(BatchProcessedEvent message) =>
            _logger.LogInformation($"Successfully processed batch '{message.BatchId} ({message.BatchSize} messages)");
    }
}
