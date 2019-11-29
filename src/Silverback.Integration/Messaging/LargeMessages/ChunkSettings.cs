// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.LargeMessages
{
    public class ChunkSettings : IEquatable<ChunkSettings>
    {
        public int Size { get; set; }

        public void Validate()
        {
            if (Size < 1)
                throw new EndpointConfigurationException("Chunk.Size must be greater or equal to 1.");
        }

        public bool Equals(ChunkSettings other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return Size == other.Size;
        }

        public override bool Equals(object obj)
        {
            if (obj is null) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != this.GetType()) return false;
            return Equals((ChunkSettings) obj);
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode() => Size;
    }
}