// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Chunking;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

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
        ///     Gets or sets the message chunking settings. This option can be used to split large messages into
        ///     smaller chunks.
        /// </summary>
        public ChunkSettings Chunk { get; set; } = new ChunkSettings();

        /// <inheritdoc cref="Endpoint.Validate" />
        public override void Validate()
        {
            base.Validate();

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
