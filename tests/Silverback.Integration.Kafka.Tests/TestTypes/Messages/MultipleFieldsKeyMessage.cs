// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.Kafka.TestTypes.Messages
{
    public class MultipleKeyMembersMessage : IMessage
    {
        public Guid Id { get; set; }
        [KafkaKeyMember]
        public string One { get; set; }
        [KafkaKeyMember]
        public string Two { get; set; }
        public string Three { get; set; }
    }
}