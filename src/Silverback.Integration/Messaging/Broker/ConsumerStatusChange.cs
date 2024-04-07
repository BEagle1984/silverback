// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Broker;

internal sealed class ConsumerStatusChange : IConsumerStatusChange
{
    public ConsumerStatusChange(ConsumerStatus status)
        : this(status, DateTime.UtcNow)
    {
    }

    public ConsumerStatusChange(ConsumerStatus status, DateTime timestamp)
    {
        Status = status;
        Timestamp = timestamp;
    }

    public ConsumerStatus Status { get; }

    public DateTime? Timestamp { get; }
}
