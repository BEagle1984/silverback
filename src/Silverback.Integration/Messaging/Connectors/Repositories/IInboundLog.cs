// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Connectors.Repositories
{
    /// <summary>
    ///     Used by the <see cref="LoggedInboundConnector" /> to keep track of each processed message and
    ///     guarantee that each one is processed only once.
    /// </summary>
    public interface IInboundLog : ITransactional
    {
        /// <summary>
        ///     Add the message contained in the specified envelope to the log.
        /// </summary>
        /// <param name="envelope">
        ///     The envelope containing the message to be added.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task Add(IRawInboundEnvelope envelope);

        /// <summary>
        ///     Returns a boolean value indicating whether this very same message has already been logged for the
        ///     same consumer group.
        /// </summary>
        /// <param name="envelope">
        ///     The envelope containing the message to be checked.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the result of the asynchronous operation. The task
        ///     result contains a value indicating whether the message was found in the log.
        /// </returns>
        Task<bool> Exists(IRawInboundEnvelope envelope);

        /// <summary>
        ///     Returns the total number of messages in the log.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the result of the asynchronous operation. The task
        ///     result contains the log length.
        /// </returns>
        Task<int> GetLength();
    }
}
