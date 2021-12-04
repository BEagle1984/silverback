// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Broker;

/// <summary>
///     The possible states of the <see cref="IConsumer" /> as exposed in the
///     <see cref="IConsumerStatusInfo" />.
/// </summary>
public enum ConsumerStatus
{
    /// <summary>
    ///     The consumer is not connected to the message broker.
    /// </summary>
    Disconnected = 0,

    /// <summary>
    ///     The consumer has successfully initialized the connection to the message broker.
    /// </summary>
    /// <remarks>
    ///     This doesn't necessary mean that it is connected and ready to consume. The underlying library might
    ///     handle the connection process asynchronously in the background or the protocol might require extra steps
    ///     (e.g. Kafka might require the partitions to be assigned).
    /// </remarks>
    Connected = 1,

    /// <summary>
    ///     The consumer is completely initialized and is ready to consume.
    /// </summary>
    /// <remarks>
    ///     This includes all extra steps that might be required by the underlying library or the protocol (e.g. a
    ///     Kafka partitions assignment has been received).
    /// </remarks>
    Ready = 2,

    /// <summary>
    ///     The consumer is connected and has received some messages.
    /// </summary>
    Consuming = 3
}
