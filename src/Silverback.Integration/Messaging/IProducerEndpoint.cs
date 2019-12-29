// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.LargeMessages;

namespace Silverback.Messaging
{
    public interface IProducerEndpoint : IEndpoint
    {
        /// <summary>
        /// Gets or sets the message chunking settings. This option can be used to split large messages
        /// into smaller chunks.
        /// </summary>
        ChunkSettings Chunk { get; }
    }
}