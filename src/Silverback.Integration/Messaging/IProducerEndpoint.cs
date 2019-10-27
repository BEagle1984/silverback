// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.LargeMessages;

namespace Silverback.Messaging
{
    public interface IProducerEndpoint : IEndpoint
    {
        ChunkSettings Chunk { get; }
    }
}