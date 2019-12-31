// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.LargeMessages;

#pragma warning disable CS0659 // Type overrides Object.Equals(object o) but does not override Object.GetHashCode()

namespace Silverback.Messaging
{
    public abstract class ProducerEndpoint : Endpoint, IProducerEndpoint
    {
        protected ProducerEndpoint(string name)
            : base(name)
        {
        }

        /// <summary>
        ///     Gets or sets the message chunking settings. This option can be used to split large messages
        ///     into smaller chunks.
        /// </summary>
        public ChunkSettings Chunk { get; set; } = new ChunkSettings();

        public override void Validate()
        {
            base.Validate();

            Chunk?.Validate();
        }

        #region Equality

        protected bool Equals(ProducerEndpoint other) => base.Equals(other) && Equals(Chunk, other.Chunk);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((ProducerEndpoint) obj);
        }

        #endregion
    }
}