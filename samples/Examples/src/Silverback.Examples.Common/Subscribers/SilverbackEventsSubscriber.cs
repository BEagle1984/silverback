// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Examples.Common.Subscribers
{
    [SuppressMessage("ReSharper", "UnusedMember.Global")]
    public class SilverbackEventsSubscriber : ISubscriber
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