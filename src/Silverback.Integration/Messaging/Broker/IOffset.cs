// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Connectors;

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
    ///         The <see cref="IComparableOffset" /> interface should be implemented whenever possible to allow
    ///         the exactly-one delivery using the <see cref="OffsetStoredInboundConnector" />.
    ///     </para>
    /// </summary>
    /// <remarks>
    ///     The classes implementing this interface should also implement a public constructor accepting key and
    ///     value as string arguments.
    /// </remarks>
    public interface IOffset
    {
        /// <summary>
        ///     Gets the unique key of the queue, topic or partition this offset belongs to.
        /// </summary>
        string Key { get; }

        /// <summary>
        ///     Gets the offset value.
        /// </summary>
        string Value { get; }

        /// <summary>
        ///     Returns a string representation of the offset suitable for logging purpose.
        /// </summary>
        /// <returns>
        ///     For an optimal readability of the logs this string should include the topic/queue subset (such as the
        ///     partition in Kafka) but not the topic/queue name (that's already logged).
        /// </returns>
        string ToLogString();
    }
}
