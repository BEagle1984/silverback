// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading.Tasks;

namespace Silverback.Util;

internal static class StreamExtensions
{
    // Returns the whole content of the stream. When the stream is an OwnedReadOnlyMemoryStream its buffer is returned without copying it,
    // so the returned array may be shared and must not be modified (nor handed to user code)
    public static ValueTask<byte[]?> ReadAllAsync(this Stream? stream)
    {
        if (stream == null)
            return ValueTask.FromResult<byte[]?>(null);

        if (stream is OwnedReadOnlyMemoryStream ownedStream)
            return ValueTask.FromResult<byte[]?>(ownedStream.Buffer);

        return ReadAllCoreAsync(stream);
    }

    // Returns the whole content of the stream. When the stream is an OwnedReadOnlyMemoryStream its buffer is returned without copying it,
    // so the returned array may be shared and must not be modified (nor handed to user code)
    public static byte[]? ReadAll(this Stream? stream)
    {
        if (stream == null)
            return null;

        if (stream is OwnedReadOnlyMemoryStream ownedStream)
            return ownedStream.Buffer;

        if (stream is MemoryStream memoryStream)
            return memoryStream.ToArray();

        using MemoryStream tempMemoryStream = new();
        stream.CopyTo(tempMemoryStream);
        return tempMemoryStream.ToArray();
    }

    public static async ValueTask<byte[]?> ReadAsync(this Stream? stream, int count)
    {
        if (stream == null)
            return null;

        byte[] buffer = new byte[count];

        int readCount = await stream.ReadAsync(buffer.AsMemory()).ConfigureAwait(false);
        return buffer[..readCount];
    }

    public static byte[]? Read(this Stream? stream, int count)
    {
        if (stream == null)
            return null;

        byte[] buffer = new byte[count];

        int readCount = stream.Read(buffer.AsSpan());
        return buffer[..readCount];
    }

    private static async ValueTask<byte[]?> ReadAllCoreAsync(Stream stream)
    {
        if (stream is MemoryStream memoryStream)
            return memoryStream.ToArray();

        using MemoryStream tempMemoryStream = new();
        await stream.CopyToAsync(tempMemoryStream).ConfigureAwait(false);
        return tempMemoryStream.ToArray();
    }
}
