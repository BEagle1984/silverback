// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Util;

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
        /// <param name="envelope">
        ///     The envelope containing the message being processed.
        /// </param>
        /// <param name="consumer">
        ///     The <see cref="IConsumer" /> that triggered this pipeline.
        /// </param>
        /// <param name="errorPolicy">
        ///     The <see cref="IErrorPolicy" /> to be applied.
        /// </param>
        [SuppressMessage("", "SA1009", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public ConsumerPipelineContext(IRawInboundEnvelope envelope, IConsumer consumer, IErrorPolicy? errorPolicy)
        {
            ErrorPolicy = errorPolicy;
            Envelope = Check.NotNull(envelope, nameof(envelope));
            Consumer = Check.NotNull(consumer, nameof(consumer));

            if (envelope.Offset != null)
                CommitOffsets = new List<IOffset> { envelope.Offset };
        }

        /// <summary>
        ///     Gets the <see cref="IConsumer" /> that triggered this pipeline.
        /// </summary>
        public IConsumer Consumer { get; }

        /// <summary>
        ///     Gets the <see cref="IErrorPolicy" /> to be applied when an exception is thrown processing the consumed
        ///     message.
        /// </summary>
        public IErrorPolicy? ErrorPolicy { get; }

        /// <summary>
        ///     Gets or sets the envelopes containing the messages being processed.
        /// </summary>
        public IRawInboundEnvelope Envelope { get; set; }

        /// <summary>
        ///     Gets or sets the collection of <see cref="IOffset" /> that will be committed if the messages are
        ///     successfully processed. The collection is initialized with the offset of the message being
        ///     consumed in this pipeline but can be modified if the commit needs to be delayed or manually
        ///     controlled.
        /// </summary>
        [SuppressMessage(
            "",
            "CA2227",
            Justification = "Has to be writable to handle commits and rollbacks correctly (see usages)")]
        public IList<IOffset>? CommitOffsets { get; set; }
    }
}
