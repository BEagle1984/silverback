// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Examples.Consumer.Subscribers
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global")]
    public class BinaryFilesSubscriber : ISubscriber
    {
        private readonly ILogger<SampleEventsSubscriber> _logger;

        public BinaryFilesSubscriber(ILogger<SampleEventsSubscriber> logger)
        {
            _logger = logger;
        }

        public void OnBinaryReceived(IBinaryFileMessage message)
        {
            _logger.LogInformation("Received Binary File {@message}", message);
        }
    }
}