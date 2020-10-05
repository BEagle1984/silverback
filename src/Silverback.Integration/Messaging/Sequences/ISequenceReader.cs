// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Sequences
{
    /// <summary>
    ///     Can recognize a message that belongs to a sequence and match it with the proper <see cref="ISequence"/> instance.
    /// </summary>
    public interface ISequenceReader
    {
        /// <summary>
        ///     Checks whether this reader can and must handle the message being processed in the specified context.
        /// </summary>
        /// <param name="context">
        ///    The current <see cref="ConsumerPipelineContext"/>.
        /// </param>
        /// <returns>
        ///    A value indicating whether this reader can and must handle the message.
        /// </returns>
        bool CanHandle(ConsumerPipelineContext context);

        /// <summary>
        ///     Returns the <see cref="ISequence"/> related to the message being processed.
        /// </summary>
        /// <param name="context">
        ///    The current <see cref="ConsumerPipelineContext"/>.
        /// </param>
        /// <returns>
        ///     The <see cref="ISequence"/> or <c>null</c> if the message is to be ignored.
        /// </returns>
        ISequence? GetSequence(ConsumerPipelineContext context);
    }
}
