// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing
{
    /// <summary>
    ///     Publishes the <see cref="IMessageStreamProvider" /> that will be subscribed as
    ///     <see cref="IMessageStreamEnumerable{TMessage}" />.
    /// </summary>
    internal interface IStreamPublisher
    {
        /// <summary>
        ///     Uses the specified <see cref="IMessageStreamProvider" /> to create the
        ///     <see cref="IMessageStreamEnumerable{TMessage}" /> to be published and returns a <see cref="Task" />
        ///     that will complete after all the subscribers complete (either because the stream was enumerated to the
        ///     end, the enumeration was aborted or an exception occurred).
        /// </summary>
        /// <param name="streamProvider">
        ///     The <see cref="IMessageStreamProvider" /> to be used to generate the streams to be published.
        /// </param>
        /// <returns>
        ///     A collection of <see cref="Task" /> that will complete when each subscriber completes.
        /// </returns>
        IReadOnlyCollection<Task> Publish(IMessageStreamProvider streamProvider);

        /// <summary>
        ///     Uses the specified <see cref="IMessageStreamProvider" /> to create the
        ///     <see cref="IMessageStreamEnumerable{TMessage}" /> to be published and returns a <see cref="Task" />
        ///     that will complete after all the subscribers complete (either because the stream was enumerated to the
        ///     end, the enumeration was aborted or an exception occurred).
        /// </summary>
        /// <param name="streamProvider">
        ///     The <see cref="IMessageStreamProvider" /> to be used to generate the streams to be published.
        /// </param>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> used to cancel the operation.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains a
        ///     collection of <see cref="Task" /> that will complete when each subscriber completes.
        /// </returns>
        Task<IReadOnlyCollection<Task>> PublishAsync(
            IMessageStreamProvider streamProvider,
            CancellationToken cancellationToken = default);
    }
}
