// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

#pragma warning disable 618

namespace Silverback.Tests.Integration.Kafka.TestTypes.Messages
{
    public class LegacyKeyMemberAttributeMessage : IMessage
    {
        public Guid Id { get; set; }

        [PartitioningKeyMember]
        public string One { get; set; }

        public string Two { get; set; }

        public string Three { get; set; }
    }
}