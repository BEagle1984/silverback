// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Lock;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Lock;

public class InMemoryLockSettingsFixture
{
    [Fact]
    public void Constructor_ShouldCreateDefaultSettings()
    {
        InMemoryLockSettings settings = new();

        settings.LockName.Should().Be("default");
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenComparingWithSameInstance()
    {
        InMemoryLockSettings settings = new();

        settings.Equals(settings).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSettingsAreEquivalent()
    {
        InMemoryLockSettings settings1 = new() { LockName = "lock" };
        InMemoryLockSettings settings2 = new() { LockName = "lock" };

        settings1.Equals(settings2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenSettingsAreDifferent()
    {
        InMemoryLockSettings settings1 = new() { LockName = "lock1" };
        InMemoryLockSettings settings2 = new() { LockName = "lock2" };

        settings1.Equals(settings2).Should().BeFalse();
    }
}
