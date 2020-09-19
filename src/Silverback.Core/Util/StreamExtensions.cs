// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading.Tasks;

namespace Silverback.Util
{
    internal static class StreamExtensions
    {
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public static async ValueTask<byte[]?> ReadAllAsync(this Stream? stream)
        {
            if (stream == null)
                return null;

            await using (var memoryStream = new MemoryStream())
            {
                await stream.CopyToAsync(memoryStream).ConfigureAwait(false);
                return memoryStream.ToArray();
            }
        }

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public static byte[]? ReadAll(this Stream? stream)
        {
            if (stream == null)
                return null;

            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public static async ValueTask<byte[]?> ReadAsync(this Stream? stream, int count)
        {
            if (stream == null)
                return null;

            var buffer = new byte[count];

            await stream.ReadAsync(buffer.AsMemory()).ConfigureAwait(false);

            return buffer;
        }

        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
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
