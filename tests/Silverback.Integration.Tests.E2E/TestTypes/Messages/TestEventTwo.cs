// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.E2E.TestTypes.Messages
{
    public class TestEventTwo : IIntegrationEvent
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
    }
}