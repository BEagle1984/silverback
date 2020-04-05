// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <summary>
    ///     The context that is passed along the producer behaviors pipeline.
    /// </summary>
    public class ProducerPipelineContext
    {
        public ProducerPipelineContext(IOutboundEnvelope envelope, IProducer producer)
        {
            Envelope = envelope;
            Producer = producer;
        }

        /// <summary>
        ///     Gets or sets the envelope containing the message to be produced.
        /// </summary>
        public IOutboundEnvelope Envelope { get; set; }

        /// <summary>
        ///     Gets the instance of <see cref="IProducer" /> that triggered this pipeline.
        /// </summary>
        public IProducer Producer { get; }
    }
}