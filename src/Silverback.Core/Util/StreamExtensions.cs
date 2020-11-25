// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using System.Threading.Tasks;

namespace Silverback.Util
{
    internal static class StreamExtensions
    {
        public static async ValueTask<byte[]?> ReadAllAsync(this Stream? stream)
        {
            if (stream == null)
                return null;

            if (stream is MemoryStream memoryStream)
                return memoryStream.ToArray();

            await using (var tempMemoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(tempMemoryStream).ConfigureAwait(false);
                return tempMemoryStream.ToArray();
            }
        }

        public static byte[]? ReadAll(this Stream? stream)
        {
            if (stream == null)
                return null;

            if (stream is MemoryStream memoryStream)
                return memoryStream.ToArray();

            using (var tempMemoryStream = new MemoryStream())
            {
                stream.CopyTo(tempMemoryStream);
                return tempMemoryStream.ToArray();
            }
        }

        public static async ValueTask<byte[]?> ReadAsync(this Stream? stream, int count)
        {
            if (stream == null)
                return null;

            var buffer = new byte[count];

            await stream.ReadAsync(buffer.AsMemory()).ConfigureAwait(false);

            return buffer;
        }

        public static byte[]? Read(this Stream? stream, int count)
        {
            if (stream == null)
                return null;

            var buffer = new byte[count];

            stream.Read(buffer.AsSpan());

            return buffer;
        }
    }
}
