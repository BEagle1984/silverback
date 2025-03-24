// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text;
using Shouldly;
using Silverback.Messaging.Serialization;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Serialization;

public class MessageEncodingExtensionsFixture
{
    [Fact]
    public void ToEncoding_ShouldReturnDefault_WhenDefault()
    {
        Encoding encoding = MessageEncoding.Default.ToEncoding();

        encoding.ShouldBe(Encoding.Default);
    }

    [Fact]
    public void ToEncoding_ShouldReturnAscii_WhenAscii()
    {
        Encoding encoding = MessageEncoding.ASCII.ToEncoding();

        encoding.ShouldBe(Encoding.ASCII);
    }

    [Fact]
    public void ToEncoding_ShouldReturnUtf8_WhenUtf8()
    {
        Encoding encoding = MessageEncoding.UTF8.ToEncoding();

        encoding.ShouldBe(Encoding.UTF8);
    }

    [Fact]
    public void ToEncoding_ShouldReturnUtf32_WhenUtf32()
    {
        Encoding encoding = MessageEncoding.UTF32.ToEncoding();

        encoding.ShouldBe(Encoding.UTF32);
    }

    [Fact]
    public void ToEncoding_ShouldReturnUnicode_WhenUnicode()
    {
        Encoding encoding = MessageEncoding.Unicode.ToEncoding();

        encoding.ShouldBe(Encoding.Unicode);
    }
}
