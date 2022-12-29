// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Lock;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Outbound.TransactionalOutbox;

public class OutboxWorkerSettingsFixture
{
    [Fact]
    public void Constructor_ShouldDeriveLockSettingsFromOutboxSettings()
    {
        OutboxWorkerSettings settings = new(new TestOutboxSettings());

        settings.DistributedLock.Should().BeOfType<TestLockSettings>();
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenLockSettingsCannotBeDerived()
    {
        Action act = () =>
        {
            OutboxWorkerSettings dummy = new(new TestOutboxSettingsNoLock());
        };

        act.Should().Throw<SilverbackConfigurationException>();
    }

    [Fact]
    public void Constructor_ShouldSetLockSettings_WhenSpecified()
    {
        OutboxWorkerSettings settings = new(new TestOutboxSettingsNoLock(), new TestLockSettings());

        settings.DistributedLock.Should().BeOfType<TestLockSettings>();
    }

    [Fact]
    public void Constructor_ShouldNotThrow_WhenNullLockIsSpecified()
    {
        OutboxWorkerSettings settings = new(new TestOutboxSettingsNoLock(), null);

        settings.DistributedLock.Should().BeNull();
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        OutboxWorkerSettings settings = new(new TestOutboxSettings());

        Action act = settings.Validate;

        act.Should().NotThrow();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenIntervalIsOutOfRange()
    {
        OutboxWorkerSettings settings = new(new TestOutboxSettings())
        {
            Interval = TimeSpan.FromSeconds(-42)
        };

        Action act = settings.Validate;

        act.Should().Throw<SilverbackConfigurationException>();
    }

    [Fact]
    public void Validate_ShouldThrow_WhenBatchSizeIsOutOfRange()
    {
        OutboxWorkerSettings settings = new(new TestOutboxSettings())
        {
            BatchSize = -42
        };

        Action act = settings.Validate;

        act.Should().Throw<SilverbackConfigurationException>();
    }

    private record TestOutboxSettings : OutboxSettings
    {
        public override DistributedLockSettings GetCompatibleLockSettings() => new TestLockSettings();
    }

    private record TestLockSettings() : DistributedLockSettings("lock");

    private record TestOutboxSettingsNoLock : OutboxSettings
    {
        public override DistributedLockSettings? GetCompatibleLockSettings() => null;
    }
}
