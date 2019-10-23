// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.HealthChecks
{
    public class PingMessage : IMessage
    {
        public static PingMessage New() => new PingMessage {TimeStamp = DateTime.UtcNow};

        public DateTime TimeStamp { get; set; }
    }
}