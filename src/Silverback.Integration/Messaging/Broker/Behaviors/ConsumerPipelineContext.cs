// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <summary>
    ///     The context that is passed along the consumer behaviors pipeline.
    /// </summary>
    public class ConsumerPipelineContext
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ConsumerPipelineContext" /> class.
        /// </summary>
        /// <param name="envelopes">
        ///     The envelopes containing the messages being processed.
        /// </param>
        /// <param name="consumer">
        ///     The <see cref="IConsumer" /> that triggered this pipeline.
        /// </param>
        /// <param name="commitOffsets">
        ///     The collection of <see cref="IOffset" /> that will be committed if the messages are
        ///     successfully processed.
        /// </param>
        public ConsumerPipelineContext(
            IReadOnlyCollection<IRawInboundEnvelope> envelopes,
            IConsumer consumer,
            IEnumerable<IOffset>? commitOffsets = null)
        {
            Envelopes = envelopes;
            Consumer = consumer;
            CommitOffsets = commitOffsets?.ToList() ?? envelopes.Select(envelope => envelope.Offset).ToList();
        }

        /// <summary>
        ///     Gets or sets the envelopes containing the messages being processed.
        /// </summary>
        public IReadOnlyCollection<IRawInboundEnvelope> Envelopes { get; set; }

        /// <summary>
        ///     Gets the <see cref="IConsumer" /> that triggered this pipeline.
        /// </summary>
        public IConsumer Consumer { get; }

        /// <summary>
        ///     Gets or sets the collection of <see cref="IOffset" /> that will be committed if the messages
        ///     are successfully processed. The collection is initialized with the offsets of all messages
        ///     being consumed in this pipeline but can be modified if the commit needs to be delayed or
        ///     manually controlled.
        /// </summary>
        [SuppressMessage(
            "ReSharper",
            "CA2227",
            Justification = "Has to be writable to handle commits and rollbacks correctly (see usages)")]
        public List<IOffset> CommitOffsets { get; set; }
    }
}
