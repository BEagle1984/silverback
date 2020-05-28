// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     The delegate passed to the <see cref="IConsumer" /> to get a callback when a message is received.
    /// </summary>
    /// <param name="args">
    ///     The <see cref="MessagesReceivedCallbackArgs" /> containing the received messages and the scoped
    ///     service provider.
    /// </param>
    public delegate void MessagesReceivedCallback(MessagesReceivedCallbackArgs args);
}
