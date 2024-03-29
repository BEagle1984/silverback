﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.E2E.TestTypes.Messages
{
    public class TestEventWithStringKafkaKey : IIntegrationEvent
    {
        [KafkaKeyMember]
        public string? KafkaKey { get; set; }

        public string? Content { get; set; }
    }
}
