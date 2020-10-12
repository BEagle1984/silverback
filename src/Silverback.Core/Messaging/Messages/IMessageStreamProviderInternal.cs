// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Relays the streamed messages to all the linked <see cref="MessageStreamEnumerable{TMessage}"/>.
    /// </summary>
    internal interface IMessageStreamProviderInternal : IMessageStreamProvider
    {
        /// <summary>
        ///     Used by the linked stream to notify the main stream that the message has been processed.
        /// </summary>
        /// <param name="pushedMessage">
        ///     The processed message.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task NotifyStreamProcessedAsync(PushedMessage pushedMessage);

        /// <summary>
        ///     Used by the linked stream to notify the main stream that the enumeration completed.
        /// </summary>
        /// <param name="stream">
        ///     The linked stream.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task NotifyStreamEnumerationCompletedAsync(IMessageStreamEnumerable stream);
    }
}
