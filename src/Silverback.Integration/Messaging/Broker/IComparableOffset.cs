// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Inbound.ExactlyOnce;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     <para>
    ///         Contains the information about the position in the messages stream being produced or consumed.
    ///     </para>
    ///     <para>
    ///         It can represent a Kafka offset or other similar constructs.
    ///     </para>
    ///     <para>
    ///         Being able to compare the offsets allows the exactly-one delivery using the
    ///         <see cref="OffsetStoreExactlyOnceStrategy" /> and for it to work properly the offsets have to be
    ///         universally comparable (across restarts and across multiple instances, for a given Key).
    ///     </para>
    /// </summary>
    // TODO: Review summary
    public interface IComparableOffset : IOffset, IComparable<IOffset>
    {
    }
}
