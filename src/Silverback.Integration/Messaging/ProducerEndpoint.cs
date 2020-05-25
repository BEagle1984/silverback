// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.LargeMessages;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

namespace Silverback.Messaging
{
    /// <inheritdoc cref="IProducerEndpoint" />
    public abstract class ProducerEndpoint : Endpoint, IProducerEndpoint
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="ProducerEndpoint" /> class.
        /// </summary>
        /// <param name="name"> The endpoint name. </param>
        protected ProducerEndpoint(string name)
            : base(name)
        {
        }

        /// <summary>
        ///     Gets or sets the message chunking settings. This option can be used to split large messages into
        ///     smaller chunks.
        /// </summary>
        public ChunkSettings Chunk { get; set; } = new ChunkSettings();

        /// <inheritdoc />
        public override void Validate()
        {
            base.Validate();

            Chunk?.Validate();
        }

        /// <summary>
        ///     Determines whether the specified <see cref="ProducerEndpoint" /> is equal to the current
        ///     <see cref="ProducerEndpoint" />.
        /// </summary>
        /// <param name="other">
        ///     The object to compare with the current object.
        /// </param>
        /// <returns>
        ///     Returns a value indicating whether the other object is equal to the current object.
        /// </returns>
        protected bool Equals(ProducerEndpoint other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return base.Equals(other) && Equals(Chunk, other.Chunk);
        }
    }
}
