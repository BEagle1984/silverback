// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.RabbitMQ.TestTypes.Messages
{
    public class MultipleRoutingKeyAttributesMessage : IMessage
    {
        public Guid Id { get; set; }

        [RabbitRoutingKey]
        public string? One { get; set; }

        [RabbitRoutingKey]
        public string? Two { get; set; }

        public string? Three { get; set; }
    }
}
