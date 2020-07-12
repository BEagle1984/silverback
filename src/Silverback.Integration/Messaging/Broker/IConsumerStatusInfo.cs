// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     Encapsulates the status details and basic statistics of an <see cref="IConsumer" />.
    /// </summary>
    public interface IConsumerStatusInfo
    {
        /// <summary>
        ///     Gets the current consumer status.
        /// </summary>
        ConsumerStatus Status { get; }

        /// <summary>
        ///     Gets the collection of <see cref="IConsumerStatusChange"/> recording all state transitions.
        /// </summary>
        IReadOnlyCollection<IConsumerStatusChange> History { get; }

        /// <summary>
        ///     Gets the total number of messages that have been consumed by the consumer instance.
        /// </summary>
        int ConsumedMessagesCount { get; }

        /// <summary>
        ///     Gets the timestamp at which the last message has been consumed.
        /// </summary>
        DateTime? LastConsumedMessageTimestamp { get; }

        /// <summary>
        ///     Gets the offset of the last consumed message.
        /// </summary>
        IOffset? LastConsumedMessageOffset { get; }
    }
}
