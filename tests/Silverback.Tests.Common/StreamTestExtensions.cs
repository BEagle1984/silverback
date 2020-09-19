// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using System.IO;

namespace Silverback.Tests
{
    public static class StreamTestExtensions
    {
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public static byte[]? ReReadAll(this Stream? stream)
        {
            if (stream == null)
                return null;

            stream.Position = 0;

            using (var memoryStream = new MemoryStream())
            {
                stream.CopyTo(memoryStream);
                return memoryStream.ToArray();
            }
        }
    }
}
