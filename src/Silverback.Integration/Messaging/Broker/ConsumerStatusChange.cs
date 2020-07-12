// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Broker
{
    internal class ConsumerStatusChange : IConsumerStatusChange
    {
        public ConsumerStatusChange(ConsumerStatus status)
        {
            Status = status;
            Timestamp = DateTime.Now;
        }

        public ConsumerStatus Status { get; }

        public DateTime? Timestamp { get; }
    }
}
