// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Text;
using Shouldly;
using Silverback.Messaging.Serialization;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Serialization;

public class MessageEncodingExtensionsTests
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

        encoding.CodePage.ShouldBe(Encoding.UTF8.CodePage);
        encoding.GetPreamble().ShouldBeEmpty();
    }

    [Fact]
    public void ToEncoding_ShouldReturnUtf32_WhenUtf32()
    {
        Encoding encoding = MessageEncoding.UTF32.ToEncoding();

        encoding.CodePage.ShouldBe(Encoding.UTF32.CodePage);
        encoding.GetPreamble().ShouldBeEmpty();
    }

    [Fact]
    public void ToEncoding_ShouldReturnUnicode_WhenUnicode()
    {
        Encoding encoding = MessageEncoding.Unicode.ToEncoding();

        encoding.CodePage.ShouldBe(Encoding.Unicode.CodePage);
        encoding.GetPreamble().ShouldBeEmpty();
    }
}
