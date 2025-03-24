// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Messaging.Configuration;

public class InMemoryOutboxSettingsBuilderFixture
{
    [Fact]
    public void Build_ShouldBuildDefaultSettings()
    {
        InMemoryOutboxSettingsBuilder builder = new();

        OutboxSettings settings = builder.Build();

        settings.ShouldBe(new InMemoryOutboxSettings());
    }

    [Fact]
    public void WithName_ShouldSetOutboxName()
    {
        InMemoryOutboxSettingsBuilder builder = new();

        OutboxSettings settings = builder.WithName("test-outbox").Build();

        settings.ShouldBeOfType<InMemoryOutboxSettings>().OutboxName.ShouldBe("test-outbox");
    }
}
