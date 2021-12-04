// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;

namespace Silverback.Tests;

public static class BytesUtil
{
    public static Stream GetRandomStream(int? length = null) => new MemoryStream(GetRandomBytes(length));

    public static byte[] GetRandomBytes(int? length = null)
    {
        Random random = new();
        length ??= random.Next(10, 50);

        byte[] bytes = new byte[length.Value];
        random.NextBytes(bytes);

        return bytes;
    }
}
