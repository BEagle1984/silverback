// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.E2E.TestTypes.Messages
{
    public class TestEventWithUniqueKey : IIntegrationEvent
    {
        [Header(DefaultMessageHeaders.MessageId)]
        public string? UniqueKey { get; set; }

        public string? Content { get; set; }
    }
}