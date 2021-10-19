// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Inbound.ExactlyOnce;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     <para>
    ///         The primary identifier used by the message broker to recognize the exact message.
    ///     </para>
    ///     <para>
    ///         It can represent a Kafka offset, a RabbitMQ delivery tag or other similar constructs.
    ///     </para>
    ///     <para>
    ///         The <see cref="IBrokerMessageOffset" /> interface should be implemented whenever possible
    ///         to allow the exactly-one delivery using the <see cref="OffsetStoreExactlyOnceStrategy" />.
    ///     </para>
    ///     <para>
    ///         If the message broker doesn't provide any message identifier, a local one can be created (e.g.
    ///         <c>Guid.NewGuid()</c>) but this will prevent some features to work properly.
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

        /// <summary>
        ///     Gets a string that can be used to log the offset value.
        /// </summary>
        /// <remarks>
        ///     This string should contain all identifiers except the endpoint name.
        /// </remarks>
        /// <returns>
        ///     A <see cref="string" /> representing the offset value.
        /// </returns>
        string ToLogString();

        /// <summary>
        ///     Gets a string that can be used to log the offset value.
        /// </summary>
        /// <remarks>
        ///     This string must include the endpoint name, if the identifier value isn't unique across
        ///     different endpoints.
        /// </remarks>
        /// <returns>
        ///     A <see cref="string" /> representing the offset value.
        /// </returns>
        string ToVerboseLogString();
    }
}
