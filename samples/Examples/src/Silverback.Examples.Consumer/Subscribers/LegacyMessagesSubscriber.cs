// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Examples.Consumer.Subscribers
{
    [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Subscriber")]
    public class LegacyMessagesSubscriber : ISubscriber
    {
        private readonly ILogger<LegacyMessagesSubscriber> _logger;

        public LegacyMessagesSubscriber(ILogger<LegacyMessagesSubscriber> logger)
        {
            _logger = logger;
        }

        [Subscribe]
        private void OnLegacyMessageReceived(LegacyMessage message)
        {
            _logger.LogInformation("Received legacy message {@message}", message);
        }
    }
}
