// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Broker
{
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
        ///     The consumer is connected to the message broker but no message has been received yet.
        /// </summary>
        Connected = 1,

        /// <summary>
        ///     The consumer is connected and has received some messages.
        /// </summary>
        Consuming = 2
    }
}
