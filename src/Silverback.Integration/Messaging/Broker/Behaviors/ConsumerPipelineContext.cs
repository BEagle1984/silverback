// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
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
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider"/> to be used to resolve the required services.
        /// </param>
        [SuppressMessage("", "SA1009", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public ConsumerPipelineContext(IRawInboundEnvelope envelope, IConsumer consumer, IServiceProvider serviceProvider)
        {
            ServiceProvider = serviceProvider;
            Envelope = Check.NotNull(envelope, nameof(envelope));
            Consumer = Check.NotNull(consumer, nameof(consumer));
        }

        /// <summary>
        ///     Gets the <see cref="IConsumer" /> that triggered this pipeline.
        /// </summary>
        public IConsumer Consumer { get; }

        /// <summary>
        ///     Gets or sets the envelopes containing the messages being processed.
        /// </summary>
        public IRawInboundEnvelope Envelope { get; set; }

        /// <summary>
        ///     Gets or sets the <see cref="IServiceProvider"/> to be used to resolve the required services.
        /// </summary>
        public IServiceProvider ServiceProvider { get; set; }
    }
}
