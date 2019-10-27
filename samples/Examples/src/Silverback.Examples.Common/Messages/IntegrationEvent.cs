// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Examples.Common.Messages
{
    public class IntegrationEvent : IIntegrationEvent
    {
        public Guid Id { get; set; }
        public string Content { get; set; }
    }
}