// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Connectors;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    /// <para>Contains the information about the position in the messages stream being produced or consumed.</para>
    /// <para>It can represent a Kafka offset or other similar constructs.</para>
    /// <para>The <see cref="IComparableOffset"/> interface should be implemented whenever possible to allow the
    /// exactly-one delivery using the <see cref="OffsetStoredInboundConnector"/>.</para>
    /// </summary>
    public interface IOffset
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
