// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Microsoft.Extensions.Logging;
using Silverback.Examples.Common.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Examples.Consumer.Subscribers
{
    [SuppressMessage("ReSharper", "ClassNeverInstantiated.Global", Justification = "Subscriber")]
    [SuppressMessage("ReSharper", "UnusedMember.Global", Justification = "Subscriber")]
    public class MultipleGroupsSubscriber
    {
        private readonly ILogger<MultipleGroupsSubscriber> _logger;

        public MultipleGroupsSubscriber(ILogger<MultipleGroupsSubscriber> logger)
        {
            _logger = logger;
        }

        [KafkaGroupIdFilter("__group-1")]
        public void OnGroup1MessageReceived(MultipleGroupsMessage message) =>
            _logger.LogInformation($"Received message '{message.Content}' from group 1");

        [KafkaGroupIdFilter("__group-2")]
        public void OnGroup2MessageReceived(MultipleGroupsMessage message) =>
            _logger.LogInformation($"Received message '{message.Content}' from group 2");
    }
}
