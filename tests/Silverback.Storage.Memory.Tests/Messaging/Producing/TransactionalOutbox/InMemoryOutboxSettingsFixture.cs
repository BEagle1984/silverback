// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Shouldly;
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

        settings.OutboxName.ShouldBe("default");
    }

    [Fact]
    public void Constructor_ShouldSetOutboxName()
    {
        InMemoryOutboxSettings settings = new("my-outbox");

        settings.OutboxName.ShouldBe("my-outbox");
    }

    [Fact]
    public void GetCompatibleLockSettings_ShouldReturnInMemoryLockSettings()
    {
        InMemoryOutboxSettings outboxSettings = new("my-outbox");

        DistributedLockSettings lockSettings = outboxSettings.GetCompatibleLockSettings();

        lockSettings.ShouldBeOfType<InMemoryLockSettings>().LockName.ShouldBe("outbox.my-outbox");
    }

    [Fact]
    public void Validate_ShouldNotThrow_WhenSettingsAreValid()
    {
        InMemoryOutboxSettings outboxSettings = new("my-outbox");

        Action act = outboxSettings.Validate;

        act.ShouldNotThrow();
    }

    [Theory]
    [InlineData("")]
    [InlineData(" ")]
    public void Validate_ShouldThrow_WhenOutboxNameIsEmptyOrWhitespace(string? outboxName)
    {
        InMemoryOutboxSettings outboxSettings = new(outboxName!);

        Action act = outboxSettings.Validate;

        act.ShouldThrow<SilverbackConfigurationException>();
    }
}
