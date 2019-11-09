// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Examples.Common.Messages
{
    public class SimpleIntegrationEvent : IntegrationEvent
    {
        [PartitioningKeyMember]
        public string Key { get; set; }
    }
}