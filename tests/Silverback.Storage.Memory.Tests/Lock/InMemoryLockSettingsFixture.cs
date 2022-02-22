// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Lock;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Lock;

public class InMemoryLockSettingsFixture
{
    [Fact]
    public void Equals_ShouldReturnTrue_WhenComparingWithSameInstance()
    {
        InMemoryLockSettings settings = new("lock");

        settings.Equals(settings).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnTrue_WhenSettingsAreEquivalent()
    {
        InMemoryLockSettings settings1 = new("lock");
        InMemoryLockSettings settings2 = new("lock");

        settings1.Equals(settings2).Should().BeTrue();
    }

    [Fact]
    public void Equals_ShouldReturnFalse_WhenSettingsAreDifferent()
    {
        InMemoryLockSettings settings1 = new("lock1");
        InMemoryLockSettings settings2 = new("lock2");

        settings1.Equals(settings2).Should().BeFalse();
    }
}
