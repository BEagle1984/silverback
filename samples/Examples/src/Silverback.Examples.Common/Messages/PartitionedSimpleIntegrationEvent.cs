// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Examples.Common.Messages
{
    public class PartitionedSimpleIntegrationEvent : IntegrationEvent
    {
        [KafkaKeyMember]
        public string Key { get; set; }
    }
}