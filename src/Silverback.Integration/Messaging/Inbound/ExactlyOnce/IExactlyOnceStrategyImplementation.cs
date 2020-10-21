// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Inbound.ExactlyOnce
{
    /// <summary>
    ///     The strategy used to guarantee that each message is consumed only once.
    /// </summary>
    public interface IExactlyOnceStrategyImplementation
    {
        /// <summary>
        ///     <para>
        ///         Checks whether the message contained in the specified envelope was already processed and must
        ///         therefore be skipped.
        ///     </para>
        ///     <para>
        ///         If the message is new, this method implicitly writes its reference to the store and enlists it
        ///         into the consumer transaction.
        ///     </para>
        /// </summary>
        /// <param name="context">
        ///     The context that is passed along the consumer behaviors pipeline.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains a
        ///     value indicating whether the message was already processed.
        /// </returns>
        Task<bool> CheckIsAlreadyProcessedAsync(ConsumerPipelineContext context);
    }
}
