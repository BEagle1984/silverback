// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Util;
using Xunit;

namespace Silverback.Tests.Core.Util;

public class HashExtensionsFixture
{
    [Theory]
    [InlineData("", "e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")]
    [InlineData("test", "9f86d081884c7d659a2feaa0c55ad015a3bf4f1b2b0b822cd15d6c15b0f00a08")]
    public void GetSha256Hash_ShouldReturnsCorrectHash(string input, string expectedHash)
    {
        string actualHash = input.GetSha256Hash();

        actualHash.ShouldBe(expectedHash);
    }
}
