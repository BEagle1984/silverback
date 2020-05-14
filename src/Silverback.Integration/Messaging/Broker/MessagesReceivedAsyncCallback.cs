// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Messaging.Broker
{
    /// <summary>
    ///     The delegate passed to the <see cref="IConsumer" /> to get a callback when a message is
    ///     received.
    /// </summary>
    /// <param name="args">
    ///     The <see cref="MessagesReceivedCallbackArgs" /> containing the received messages and the
    ///     scoped service provider.
    /// </param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public delegate Task MessagesReceivedAsyncCallback(MessagesReceivedCallbackArgs args);
}
