// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;

namespace Silverback.Messaging.Sequences
{
    /// <summary>
    ///     Can recognize a message that belongs to a sequence and match it with the proper
    ///     <see cref="ISequence" /> instance.
    /// </summary>
    public interface ISequenceReader
    {
        /// <summary>
        ///     Gets a value indicating whether this reader handles the raw messages, before they are being
        ///     deserialized, decrypted, etc.
        /// </summary>
        bool HandlesRawMessages { get; }

        /// <summary>
        ///     Checks whether this reader can and must handle the message being processed in the specified context.
        /// </summary>
        /// <param name="context">
        ///     The current <see cref="ConsumerPipelineContext" />.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains a
        ///     value indicating whether this reader can and must handle the message.
        /// </returns>
        Task<bool> CanHandleAsync(ConsumerPipelineContext context);

        /// <summary>
        ///     Returns the <see cref="ISequence" /> related to the message being processed.
        /// </summary>
        /// <param name="context">
        ///     The current <see cref="ConsumerPipelineContext" />.
        /// </param>
        /// <returns>
        ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
        ///     <see cref="ISequence" />.
        /// </returns>
        Task<ISequence> GetSequenceAsync(ConsumerPipelineContext context);
    }
}
