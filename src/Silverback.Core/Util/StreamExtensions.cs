// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading.Tasks;

namespace Silverback.Util;

internal static class StreamExtensions
{
    // Returns the content of the stream from its current position to its end. A MemoryStream is read without advancing its position, so
    // that reading it again returns the same bytes. When the stream is an OwnedReadOnlyMemoryStream at its start, its buffer is returned
    // without copying it, so the returned array may be shared and must not be modified (nor handed to user code)
    public static ValueTask<byte[]?> ReadAllAsync(this Stream? stream)
    {
        if (stream == null)
            return ValueTask.FromResult<byte[]?>(null);

        if (stream is MemoryStream memoryStream)
            return ValueTask.FromResult<byte[]?>(ReadRemainingBytes(memoryStream));

        return ReadAllCoreAsync(stream);
    }

    // Returns the content of the stream from its current position to its end. A MemoryStream is read without advancing its position, so
    // that reading it again returns the same bytes. When the stream is an OwnedReadOnlyMemoryStream at its start, its buffer is returned
    // without copying it, so the returned array may be shared and must not be modified (nor handed to user code)
    public static byte[]? ReadAll(this Stream? stream)
    {
        if (stream == null)
            return null;

        if (stream is MemoryStream memoryStream)
            return ReadRemainingBytes(memoryStream);

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
        using MemoryStream tempMemoryStream = new();
        await stream.CopyToAsync(tempMemoryStream).ConfigureAwait(false);
        return tempMemoryStream.ToArray();
    }

    private static byte[] ReadRemainingBytes(MemoryStream memoryStream)
    {
        // A disposed stream has no position anymore, but its content is still available: return all of it
        long position = memoryStream.CanSeek ? memoryStream.Position : 0;
        byte[]? ownedBuffer = (memoryStream as OwnedReadOnlyMemoryStream)?.Buffer;

        if (position == 0)
            return ownedBuffer ?? memoryStream.ToArray();

        if (position >= memoryStream.Length)
            return [];

        if (ownedBuffer != null)
            return ownedBuffer[(int)position..];

        if (memoryStream.TryGetBuffer(out ArraySegment<byte> buffer))
            return [.. buffer[(int)position..]];

        // The buffer isn't exposed: copy it all and slice it, rather than reading the stream, which would change its position while
        // other readers (e.g. parallel subscribers) may be using it
        return memoryStream.ToArray()[(int)position..];
    }
}
