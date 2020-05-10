// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.LargeMessages
{
    /// <summary>
    ///     The chunking settings. To enable chunking just set the <c> Size </c> property to the desired
    ///     (maximum) chunk size.
    /// </summary>
    public class ChunkSettings : IEquatable<ChunkSettings>, IValidatableEndpointSettings
    {
        /// <summary>
        ///     Gets or sets the size in bytes of each chunk.
        /// </summary>
        public int Size { get; set; } = int.MaxValue;

        /// <inheritdoc />
        public void Validate()
        {
            if (Size < 1)
                throw new EndpointConfigurationException("Chunk.Size must be greater or equal to 1.");
        }

        /// <inheritdoc />
        public bool Equals(ChunkSettings other)
        {
            if (other is null)
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return Size == other.Size;
        }

        /// <inheritdoc />
        public override bool Equals(object obj)
        {
            if (obj is null)
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((ChunkSettings)obj);
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode", Justification = Justifications.Settings)]
        public override int GetHashCode() => Size;
    }
}
