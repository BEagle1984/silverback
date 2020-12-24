// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Outbound;
using Silverback.Messaging.Sequences.Chunking;

namespace Silverback.Messaging
{
    /// <inheritdoc cref="IProducerEndpoint" />
    public abstract class ProducerEndpoint : Endpoint, IProducerEndpoint
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ProducerEndpoint" /> class.
        /// </summary>
        /// <param name="name">
        ///     The endpoint name.
        /// </param>
        protected ProducerEndpoint(string name)
            : base(name)
        {
        }

        /// <summary>
        ///     Gets or sets the strategy to be used to produce the messages. If no strategy is specified, the
        ///     messages will be sent to the message broker directly.
        /// </summary>
        public IProduceStrategy Strategy { get; set; } = new DefaultProduceStrategy();

        /// <summary>
        ///     Gets or sets the message chunking settings. This option can be used to split large messages into
        ///     smaller chunks.
        /// </summary>
        public ChunkSettings? Chunk { get; set; }

        /// <inheritdoc cref="Endpoint.Validate" />
        public override void Validate()
        {
            base.Validate();

            if (Strategy == null)
                throw new EndpointConfigurationException("Strategy cannot be null.");

            Chunk?.Validate();
        }

        /// <inheritdoc cref="Endpoint.BaseEquals" />
        protected override bool BaseEquals(Endpoint? other)
        {
            if (ReferenceEquals(this, other))
                return true;

            if (!(other is ProducerEndpoint otherProducerEndpoint))
                return false;

            return base.BaseEquals(other) && Equals(Chunk, otherProducerEndpoint.Chunk);
        }
    }
}
