// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Sequences.Chunking;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Sequences.Chunking;

public class ChunkSettingsTests
{
    [Theory]
    [InlineData(1)]
    [InlineData(42)]
    [InlineData(int.MaxValue)]
    public void Validate_ShouldNotThrow_WhenSizeIsValid(int size)
    {
        ChunkSettings settings = new()
        {
            Size = size
        };

        Action act = settings.Validate;

        act.ShouldNotThrow();
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenDefault()
    {
        ChunkSettings settings = new();

        Action act = settings.Validate;

        act.ShouldNotThrow();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    [InlineData(int.MinValue)]
    public void Validate_ShouldThrow_WhenSizeIsLowerThanOne(int size)
    {
        ChunkSettings settings = new()
        {
            Size = size
        };

        Action act = settings.Validate;

        act.ShouldThrow<BrokerConfigurationException>().Message.ShouldBe("Chunk.Size must be greater than or equal to 1.");
    }
}
