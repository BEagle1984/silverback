// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Callbacks;

namespace Silverback.Messaging.Configuration.Kafka;

/// <summary>
///     Specifies whether the consumer has to catch up with its assigned partitions, consuming each of them until its end, and what to do
///     once it's caught up.
/// </summary>
/// <remarks>
///     The consumer is caught up when every partition assigned to it reached its end and all its messages are processed.
///     <see cref="IKafkaConsumer.WaitUntilCaughtUpAsync" /> then completes and the <see cref="IKafkaConsumerCaughtUpCallback" /> is
///     invoked.
/// </remarks>
public enum CatchUpMode
{
    /// <summary>
    ///     The consumer doesn't track whether it's caught up.
    /// </summary>
    None,

    /// <summary>
    ///     The consumer keeps consuming once it's caught up.
    /// </summary>
    KeepConsuming,

    /// <summary>
    ///     The consumer pauses each partition as soon as it reaches its end and disconnects once it's caught up.
    /// </summary>
    Disconnect
}
