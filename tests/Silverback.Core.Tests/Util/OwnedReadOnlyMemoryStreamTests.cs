// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.IO;
using Shouldly;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class OwnedReadOnlyMemoryStreamTests
{
    [Fact]
    public void Constructor_ShouldWrapWholeBuffer()
    {
        byte[] buffer = [0x01, 0x02, 0x03];

        OwnedReadOnlyMemoryStream stream = new(buffer);

        stream.Buffer.ShouldBeSameAs(buffer);
        stream.Length.ShouldBe(3);
        stream.Position.ShouldBe(0);
        stream.ToArray().ShouldBe([0x01, 0x02, 0x03]);
    }

    [Fact]
    public void Write_ShouldThrow()
    {
        OwnedReadOnlyMemoryStream stream = new([0x01, 0x02, 0x03]);

        stream.CanWrite.ShouldBeFalse();
        Should.Throw<NotSupportedException>(() => stream.WriteByte(0x04));
    }

    [Fact]
    public void GetBuffer_ShouldThrow()
    {
        MemoryStream stream = new OwnedReadOnlyMemoryStream([0x01, 0x02, 0x03]);

        Should.Throw<UnauthorizedAccessException>(stream.GetBuffer);
        stream.TryGetBuffer(out _).ShouldBeFalse();
    }
}
