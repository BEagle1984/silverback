// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;

namespace Silverback.Messaging.Broker;

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
    ///     Gets the collection of <see cref="IConsumerStatusChange" /> recording all state transitions.
    /// </summary>
    IReadOnlyCollection<IConsumerStatusChange> History { get; }

    /// <summary>
    ///     Gets the total number of messages that have been consumed by the consumer instance.
    /// </summary>
    int ConsumedMessagesCount { get; }

    /// <summary>
    ///     Gets the timestamp at which the latest message has been consumed.
    /// </summary>
    DateTime? LatestConsumedMessageTimestamp { get; }

    /// <summary>
    ///     Gets the message identifier of the latest consumed message.
    /// </summary>
    IBrokerMessageIdentifier? LatestConsumedMessageIdentifier { get; }
}
