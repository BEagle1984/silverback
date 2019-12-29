// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Connectors;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// <para>Contains the information about the position in the messages stream being produced or consumed.</para>
    /// <para>It can represent a Kafka offset or other similar constructs.</para>
    /// <para>Being able to determine the offset of the messages being produced is useful for tracing but being
    /// able to precisely recognize the offset of the message being consumed is crucial to efficiently ensure
    /// the exactly-one delivery (see <see cref="OffsetStoredInboundConnector"/>).</para>
    /// </summary>
    public interface IOffset : IComparable<IOffset>
    {
        /// <summary>
        /// The unique key of the queue/topic/partition this offset belongs to.
        /// </summary>
        string Key { get; }

        /// <summary>
        /// The offset value.
        /// </summary>
        string Value { get; }
    }
}
