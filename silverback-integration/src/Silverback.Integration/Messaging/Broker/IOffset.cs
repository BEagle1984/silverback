// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Broker
{
    /// <summary>Can represent a Kafka offset or other similar constructs such as delivery-tag in AMQP.</summary>
    public interface IOffset : IComparable
    {
        /// <summary>The unique key of the queue/topic/partition this offset belongs to.</summary>
        string Key { get; }

        string Value { get; }
    }
}
