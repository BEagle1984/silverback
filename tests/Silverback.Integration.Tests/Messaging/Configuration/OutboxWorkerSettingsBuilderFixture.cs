// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
using Silverback.Configuration;
using Silverback.Lock;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public class OutboxWorkerSettingsBuilderFixture
{
    [Fact]
    public void Build_ShouldReturnSettings()
    {
        OutboxWorkerSettingsBuilder builder = new();

        OutboxWorkerSettings settings = builder
            .ProcessOutbox(_ => new TestOutboxSettingsBuilder())
            .Build();

        settings.ShouldNotBeNull();
    }

    [Fact]
    public void Build_ShouldThrow_WhenOutboxSettingsNotSpecified()
    {
        OutboxWorkerSettingsBuilder builder = new();

        Action act = () => builder.Build();

        act.ShouldThrow<SilverbackConfigurationException>();
    }

    [Fact]
    public void ProcessOutbox_ShouldSetOutboxSettings()
    {
        OutboxWorkerSettingsBuilder builder = new();

        OutboxWorkerSettings settings = builder
            .ProcessOutbox(_ => new TestOutboxSettingsBuilder())
            .Build();

        settings.ShouldNotBeNull();
    }

    [Fact]
    public void WithInterval_ShouldSetInterval()
    {
        OutboxWorkerSettingsBuilder builder = new();

        OutboxWorkerSettings settings = builder
            .ProcessOutbox(_ => new TestOutboxSettingsBuilder())
            .WithInterval(TimeSpan.FromSeconds(42))
            .Build();

        settings.Interval.ShouldBe(TimeSpan.FromSeconds(42));
    }

    [Fact]
    public void WithInterval_ShouldThrow_WhenIntervalIsOutOfRange()
    {
        OutboxWorkerSettingsBuilder builder = new();

        Action act = () => builder
            .ProcessOutbox(_ => new TestOutboxSettingsBuilder())
            .WithInterval(TimeSpan.FromSeconds(-42));

        act.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WithIncrementalRetryDelay_ShouldSetInitialDelayAndIncrement()
    {
        OutboxWorkerSettingsBuilder builder = new();

        OutboxWorkerSettings settings = builder
            .ProcessOutbox(_ => new TestOutboxSettingsBuilder())
            .WithIncrementalRetryDelay(TimeSpan.FromMinutes(42), TimeSpan.FromDays(42))
            .Build();

        settings.InitialRetryDelay.ShouldBe(TimeSpan.FromMinutes(42));
        settings.RetryDelayIncrement.ShouldBe(TimeSpan.FromDays(42));
        settings.RetryDelayFactor.ShouldBe(1.0);
        settings.MaxRetryDelay.ShouldBeNull();
    }

    [Fact]
    public void WithIncrementalRetryDelay_ShouldSetInitialDelayAndIncrementAndMaxDelay()
    {
        OutboxWorkerSettingsBuilder builder = new();

        OutboxWorkerSettings settings = builder
            .ProcessOutbox(_ => new TestOutboxSettingsBuilder())
            .WithIncrementalRetryDelay(TimeSpan.FromMinutes(42), TimeSpan.FromDays(42), TimeSpan.FromHours(42))
            .Build();

        settings.InitialRetryDelay.ShouldBe(TimeSpan.FromMinutes(42));
        settings.RetryDelayIncrement.ShouldBe(TimeSpan.FromDays(42));
        settings.RetryDelayFactor.ShouldBe(1.0);
        settings.MaxRetryDelay.ShouldBe(TimeSpan.FromHours(42));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-42)]
    public void WithIncrementalRetryDelay_ShouldThrow_WhenInitialDelayIsLowerOrEqualToZero(int value)
    {
        OutboxWorkerSettingsBuilder builder = new();

        Action act = () => builder.WithIncrementalRetryDelay(TimeSpan.FromMinutes(value), TimeSpan.MaxValue);

        act.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-42)]
    public void WithIncrementalRetryDelay_ShouldThrow_WhenDelayIncrementIsLowerOrEqualToZero(int value)
    {
        OutboxWorkerSettingsBuilder builder = new();

        Action act = () => builder.WithIncrementalRetryDelay(TimeSpan.MaxValue, TimeSpan.FromMinutes(value));

        act.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-42)]
    public void WithIncrementalRetryDelay_ShouldThrow_WhenMaxDelayIsLowerOrEqualToZero(int value)
    {
        OutboxWorkerSettingsBuilder builder = new();

        Action act = () => builder.WithIncrementalRetryDelay(TimeSpan.MaxValue, TimeSpan.FromMinutes(42), TimeSpan.FromMinutes(value));

        act.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WithExponentialRetryDelay_ShouldSetInitialDelayAndFactor()
    {
        OutboxWorkerSettingsBuilder builder = new();

        OutboxWorkerSettings settings = builder
            .ProcessOutbox(_ => new TestOutboxSettingsBuilder())
            .WithExponentialRetryDelay(TimeSpan.FromMinutes(42), 2.0)
            .Build();

        settings.InitialRetryDelay.ShouldBe(TimeSpan.FromMinutes(42));
        settings.RetryDelayIncrement.ShouldBe(TimeSpan.Zero);
        settings.RetryDelayFactor.ShouldBe(2.0);
        settings.MaxRetryDelay.ShouldBeNull();
    }

    [Fact]
    public void WithExponentialRetryDelay_ShouldSetInitialDelayAndFactorAndMaxDelay()
    {
        OutboxWorkerSettingsBuilder builder = new();

        OutboxWorkerSettings settings = builder
            .ProcessOutbox(_ => new TestOutboxSettingsBuilder())
            .WithExponentialRetryDelay(TimeSpan.FromMinutes(42), 2.0, TimeSpan.FromHours(42))
            .Build();

        settings.InitialRetryDelay.ShouldBe(TimeSpan.FromMinutes(42));
        settings.RetryDelayIncrement.ShouldBe(TimeSpan.Zero);
        settings.RetryDelayFactor.ShouldBe(2.0);
        settings.MaxRetryDelay.ShouldBe(TimeSpan.FromHours(42));
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-42)]
    public void WithExponentialRetryDelay_ShouldThrow_WhenInitialDelayIsLowerOrEqualToZero(int value)
    {
        OutboxWorkerSettingsBuilder builder = new();

        Action act = () => builder.WithExponentialRetryDelay(TimeSpan.FromMinutes(value), 2.0);

        act.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-42)]
    public void WithExponentialRetryDelay_ShouldThrow_WhenDelayFactorIsLowerOrEqualToZero(int value)
    {
        OutboxWorkerSettingsBuilder builder = new();

        Action act = () => builder.WithExponentialRetryDelay(TimeSpan.FromMinutes(42), value);

        act.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void EnforceMessageOrder_ShouldSetEnforceMessageOrder()
    {
        OutboxWorkerSettingsBuilder builder = new();

        OutboxWorkerSettings settings = builder
            .ProcessOutbox(_ => new TestOutboxSettingsBuilder())
            .EnforceMessageOrder()
            .Build();

        settings.EnforceMessageOrder.ShouldBeTrue();
    }

    [Fact]
    public void DisableMessageOrderEnforcement_ShouldSetEnforceMessageOrder()
    {
        OutboxWorkerSettingsBuilder builder = new();

        OutboxWorkerSettings settings = builder
            .ProcessOutbox(_ => new TestOutboxSettingsBuilder())
            .DisableMessageOrderEnforcement()
            .Build();

        settings.EnforceMessageOrder.ShouldBeFalse();
    }

    [Fact]
    public void WithBatchSize_ShouldSetBatchSize()
    {
        OutboxWorkerSettingsBuilder builder = new();

        OutboxWorkerSettings settings = builder
            .ProcessOutbox(_ => new TestOutboxSettingsBuilder())
            .WithBatchSize(42)
            .Build();

        settings.BatchSize.ShouldBe(42);
    }

    [Fact]
    public void WithBatchSize_ShouldThrow_WhenBatchSizeIsOutOfRange()
    {
        OutboxWorkerSettingsBuilder builder = new();

        Action act = () => builder
            .ProcessOutbox(_ => new TestOutboxSettingsBuilder())
            .WithBatchSize(0);

        act.ShouldThrow<ArgumentOutOfRangeException>();
    }

    [Fact]
    public void WithDistributedLock_ShouldSetDistributedLock()
    {
        OutboxWorkerSettingsBuilder builder = new();

        OutboxWorkerSettings settings = builder
            .ProcessOutbox(_ => new TestOutboxSettingsBuilder())
            .WithDistributedLock(_ => new TestLockSettingsBuilder())
            .Build();

        settings.DistributedLock.ShouldBeOfType<TestLockSettings>();
    }

    [Fact]
    public void WithoutDistributedLock_ShouldSetDistributedLock()
    {
        OutboxWorkerSettingsBuilder builder = new();

        OutboxWorkerSettings settings = builder
            .ProcessOutbox(_ => new TestOutboxSettingsBuilder())
            .WithoutDistributedLock()
            .Build();

        settings.DistributedLock.ShouldBeNull();
    }

    private record TestOutboxSettings : OutboxSettings
    {
        public override DistributedLockSettings GetCompatibleLockSettings() => new TestLockSettings();
    }

    private record TestLockSettings() : DistributedLockSettings("test");

    private class TestOutboxSettingsBuilder : IOutboxSettingsImplementationBuilder
    {
        public OutboxSettings Build() => new TestOutboxSettings();
    }

    private class TestLockSettingsBuilder : IDistributedLockSettingsImplementationBuilder
    {
        public DistributedLockSettings Build() => new TestLockSettings();
    }
}
