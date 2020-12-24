// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     Represent a future <see cref="IMessageStreamEnumerable{TMessage}" />, that will created as soon as the
    ///     first message is pushed.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages being streamed.
    /// </typeparam>
    public interface ILazyMessageStreamEnumerable<out TMessage>
    {
        /// <summary>
        ///     Gets the <see cref="IMessageStreamEnumerable{TMessage}" />, as soon as it is created.
        /// </summary>
        IMessageStreamEnumerable<TMessage>? Stream { get; }

        /// <summary>
        ///     Gets an awaitable <see cref="Task" /> that completes when the first message is pushed and the
        ///     <see cref="IMessageStreamEnumerable{TMessage}" /> is created. The created stream can be retrieved via
        ///     the <see cref="Stream" /> property.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task WaitUntilCreatedAsync();
    }
}
