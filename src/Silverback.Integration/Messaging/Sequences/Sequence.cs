// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Streaming
{
    /// <summary>
    ///     Represent a stream of messages being consumed from an inbound endpoint and published through the internal bus. It is an enumerable that is asynchronously pushed with messages.
    /// </summary>
    /// <typeparam name="TMessage">
    ///     The type of the messages being streamed.
    /// </typeparam>
    public interface IInboundMessageStreamEnumerable<out TMessage> : IMessageStreamEnumerable<TMessage>
    {
        /// <summary>
        ///     <param>
        ///         Confirms that the current message has been successfully processed.
        ///     </param>
        ///     <param>
        ///         The acknowledgement will be sent to the message broker and the message will never be processed
        ///         again (by the same logical consumer / consumer group).
        ///     </param>
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task Commit();

        /// <summary>
        ///     <param>
        ///         Notifies that an error occured while processing the current message.
        ///     </param>
        ///     <param>
        ///         If necessary the information will be sent to the message broker to ensure that the message will
        ///         be re-processed.
        ///     </param>
        /// </summary>
        /// <param name="exception">
        ///    The <see cref="Exception"/> that was thrown while processing the message, if any.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        Task Rollback(Exception? exception);
    }

    internal  class InboundMessageStreamEnumerable<TMessage> : MessageStreamEnumerable<TMessage>, IInboundMessageStreamEnumerable<TMessage>
    {
        private readonly IConsumer _consumer;

        private readonly InboundMessageStreamEnumerable<IRawInboundEnvelope>? _sourceStream;

        public InboundMessageStreamEnumerable(IConsumer consumer, IInboundMessageStreamEnumerable<IRawInboundEnvelope>? sourceStream)
        {
            _consumer = Check.NotNull(consumer, nameof(consumer));
            _sourceStream = sourceStream;
        }

        public Task Commit()
        {
            if (_sourceStream != null)
                return _sourceStream.Commit();

            _consumer.Commit()
        }

        public Task Rollback(Exception? exception)
        {
            if (_sourceStream != null)
                return _sourceStream.Rollback();
        }
    }

    internal class RawInboundEnvelopeStreamEnumerable : MessageStreamEnumerable<IRawInboundEnvelope>
    {
        public RawInboundEnvelopeStreamEnumerable(int bufferCapacity = 1) : base(bufferCapacity)
        {
        }

        protected override IMessageStreamEnumerable<TMessageLinked> CreateLinkedStreamCore<TMessageLinked>(int bufferCapacity) => new RawInboundEnvelopeStreamEnumerable(bufferCapacity);
    }

    internal class RawInboundEnvelopeSequenceStramEnumerable : RawInboundEnvelopeStreamEnumerable
    {
     public RawInboundEnvelopeSequenceStramEnumerable(int bufferCapacity = 1) : base(bufferCapacity)
        {
        }
    }

    public class Sequence
    {
    }
}
