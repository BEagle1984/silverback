// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Inbound.ExactlyOnce;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     <para>
    ///         Represents the primary identifier used by the message broker to recognize the exact message.
    ///     </para>
    ///     <para>
    ///         It can represent a Kafka offset, a RabbitMQ delivery tag or other similar constructs.
    ///     </para>
    ///     <para>
    ///         The <see cref="IBrokerMessageOffset" /> interface should be implemented whenever possible
    ///         to allow the exactly-one delivery using the <see cref="OffsetStoreExactlyOnceStrategy" />.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     The classes implementing this interface should also implement a public constructor accepting key and
    ///     value as string arguments.
    /// </remarks>
    public interface IBrokerMessageIdentifier : IEquatable<IBrokerMessageIdentifier>
    {
        /// <summary>
        ///     Gets the unique key of the queue, topic or partition the message was produced to or consumed from.
        /// </summary>
        string Key { get; }

        /// <summary>
        ///     Gets the identifier value.
        /// </summary>
        string Value { get; }
    }
}
