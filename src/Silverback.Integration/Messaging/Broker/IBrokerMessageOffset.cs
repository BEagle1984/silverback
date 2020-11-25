// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Inbound.ExactlyOnce;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     <para>
    ///         Represents the primary identifier used by the message broker to recognize the exact message. This
    ///         is different than the base <see cref="IBrokerMessageIdentifier" /> because it has a ordinal
    ///         meaning and is comparable.
    ///     </para>
    ///     <para>
    ///         It can represent a Kafka offset or other similar constructs.
    ///     </para>
    ///     <para>
    ///         Being able to compare the identifiers (offsets) allows the exactly-one delivery using the
    ///         <see cref="OffsetStoreExactlyOnceStrategy" /> and for it to work properly the offsets have to be
    ///         universally comparable (across restarts and across multiple instances, for a given Key).
    ///     </para>
    /// </summary>
    public interface IBrokerMessageOffset
        : IBrokerMessageIdentifier, IComparable<IBrokerMessageOffset>
    {
    }
}
