// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using Silverback.Tests.Integration.E2E.TestTypes.Messages;
using Silverback.Tests.Integration.E2E.Util;

namespace Silverback.Tests.Integration.E2E.TestTypes;

public static class ChunksHelper
{
    public static byte[][] SerializeAndSplitIntoChunks(string content, int chunksCount)
    {
        byte[] rawMessage = DefaultSerializers.Json.SerializeToBytes(new TestEventOne { ContentEventOne = content });
        int chunkSize = (rawMessage.Length + chunksCount - 1) / chunksCount;

        return [.. rawMessage.Chunk(chunkSize)];
    }
}
