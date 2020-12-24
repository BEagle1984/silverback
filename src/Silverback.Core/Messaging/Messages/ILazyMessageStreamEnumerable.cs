// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Represent a future <see cref="IMessageStreamEnumerable{TMessage}" />, that will created as soon
    ///     as the first message is pushed.
    /// </summary>
    internal interface ILazyMessageStreamEnumerable
    {
        /// <summary>
        ///     Gets the type of the messages being streamed.
        /// </summary>
        Type MessageType { get; }

        /// <summary>
        ///     Gets the <see cref="IMessageStreamEnumerable{TMessage}" />, as soon as it is created.
        /// </summary>
        IMessageStreamEnumerable? Stream { get; }

        /// <summary>
        ///     Gets an awaitable <see cref="Task" /> that completes when the first message is pushed and the
        ///     <see cref="IMessageStreamEnumerable" /> is created. The created stream can be retrieved via
        ///     the <see cref="Stream" /> property.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation. The task result
        ///     contains the <see cref="IMessageStreamEnumerable" />.
        /// </returns>
        Task WaitUntilCreatedAsync();

        /// <summary>
        ///     Gets the already created <see cref="IMessageStreamEnumerable" /> or creates it on the fly.
        /// </summary>
        /// <returns>
        ///     The stream.
        /// </returns>
        IMessageStreamEnumerable GetOrCreateStream();

        /// <summary>
        ///     Signals that the stream will not be created anymore.
        /// </summary>
        void Cancel();
    }
}
