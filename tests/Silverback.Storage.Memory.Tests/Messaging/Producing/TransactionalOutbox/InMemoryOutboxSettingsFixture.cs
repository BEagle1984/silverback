// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using FluentAssertions;
using Silverback.Lock;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Messaging.Producing.TransactionalOutbox;

public class InMemoryOutboxSettingsFixture
{
    [Fact]
    public void Constructor_ShouldCreateDefaultSettings()
    {
        InMemoryOutboxSettings settings = new();

        settings.OutboxName.Should().Be("default");
    }

    [Fact]
    public void Constructor_ShouldSetOutboxName()
    {
        InMemoryOutboxSettings settings = new("my-outbox");

        settings.OutboxName.Should().Be("my-outbox");
    }

    [Fact]
    public void GetCompatibleLockSettings_ShouldReturnInMemoryLockSettings()
    {
        InMemoryOutboxSettings outboxSettings = new("my-outbox");

        DistributedLockSettings lockSettings = outboxSettings.GetCompatibleLockSettings();

        lockSettings.Should().BeOfType<InMemoryLockSettings>();
        lockSettings.As<InMemoryLockSettings>().LockName.Should().Be("outbox.my-outbox");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        InMemoryOutboxSettings outboxSettings = new("my-outbox");

        Action act = outboxSettings.Validate;

        act.Should().NotThrow();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenOutboxNameIsNullOrWhitespace(string? outboxName)
    {
        InMemoryOutboxSettings outboxSettings = new(outboxName!);

        Action act = outboxSettings.Validate;

        act.Should().Throw<SilverbackConfigurationException>();
    }
}
