// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     Encapsulates the information about the consumer status transition.
    /// </summary>
    public interface IConsumerStatusChange
    {
        /// <summary>
        ///     Gets the status into which the consumer has transitioned.
        /// </summary>
        ConsumerStatus Status { get; }

        /// <summary>
        ///     Gets the timestamp at which the consumer transitioned to this status.
        /// </summary>
        DateTime? Timestamp { get; }
    }
}
