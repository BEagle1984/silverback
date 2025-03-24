// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Lock;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Lock;

public class InMemoryLockSettingsFixture
{
    [Fact]
    public void Constructor_ShouldSetLockName()
    {
        InMemoryLockSettings settings = new("my-lock");

        settings.LockName.ShouldBe("my-lock");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        InMemoryLockSettings settings = new("my-lock");

        Action act = settings.Validate;

        act.ShouldNotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenLockNameIsNullOrWhitespace(string? lockName)
    {
        InMemoryLockSettings settings = new(lockName!);

        Action act = settings.Validate;

        act.ShouldThrow<SilverbackConfigurationException>();
    }
}
