// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Lock;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Lock;

public class InMemoryLockSettingsFixture
{
    [Fact]
    public void Constructor_ShouldSetLockName()
    {
        InMemoryLockSettings settings = new("my-lock");

        settings.LockName.Should().Be("my-lock");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        InMemoryLockSettings settings = new("my-lock");

        Action act = settings.Validate;

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenLockNameIsNullOrWhitespace(string? lockName)
    {
        InMemoryLockSettings settings = new(lockName!);

        Action act = settings.Validate;

        act.Should().Throw<SilverbackConfigurationException>();
    }
}
