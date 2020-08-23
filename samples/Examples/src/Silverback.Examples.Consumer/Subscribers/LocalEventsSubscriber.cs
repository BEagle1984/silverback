// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Silverback.Examples.Common.Messages;

namespace Silverback.Examples.Consumer.Subscribers
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Subscriber")]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Subscriber")]
    public class LocalEventsSubscriber
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
