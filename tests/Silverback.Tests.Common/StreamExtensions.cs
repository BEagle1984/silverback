// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Silverback.Tests;

public static class StreamExtensions
{
    [return: NotNullIfNotNull(nameof(stream))]
    public static byte[]? ReReadAll(this Stream? stream)
    {
        if (stream == null)
            return null;

        stream.Position = 0;

        using MemoryStream memoryStream = new();
        stream.CopyTo(memoryStream);
        return memoryStream.ToArray();
    }
}
