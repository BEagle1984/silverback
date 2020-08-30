// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading;
using System.Threading.Tasks;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Declares the PushAsync method, used to push to the linked streams.
    /// </summary>
    internal interface IPushableStream
    {
        /// <summary>
        ///     Add the specified message to the stream.
        /// </summary>
        /// <param name="message">
        ///     The message to be added.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the write operation.
        /// </param>
        /// <returns>
        ///     A <see cref="ValueTask" /> representing the asynchronous operation.
        /// </returns>
        ValueTask PushAsync(object message, CancellationToken cancellationToken = default);

        /// <summary>
        ///     Marks the stream as complete, meaning no more messages will be pushed. This will also end the
        ///     enumerator loop.
        /// </summary>
        void Complete();
    }
}
