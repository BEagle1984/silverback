// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.E2E.TestTypes.Messages
{
    public class TestPrioritizedCommand : IIntegrationCommand
    {
        public enum PriorityEnum
        {
            Low,
            Normal,
            High
        };

        public Guid Id { get; set; }
        public PriorityEnum Priority { get; set; }
        public string Content { get; set; }
    }
}