// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.Logging;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Examples.Consumer.Subscribers
{
    public class LocalEventsSubscriber : ISubscriber
    {
        private readonly ILogger<LocalEventsSubscriber> _logger;

        public LocalEventsSubscriber(ILogger<LocalEventsSubscriber> logger)
        {
            _logger = logger;
        }

        public void OnMove(MessageMovedEvent message) =>
            _logger.LogInformation($"Message(s) moved: {message}", message);
    }
}
