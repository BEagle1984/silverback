// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Outbound.TransactionalOutbox;
using Xunit;

namespace Silverback.Tests.Storage.Memory.Messaging.Configuration;

public class InMemoryOutboxSettingsBuilderFixture
{
    [Fact]
    public void Build_ShouldBuildDefaultSettings()
    {
        InMemoryOutboxSettingsBuilder builder = new();

        OutboxSettings settings = builder.Build();

        settings.Should().BeOfType<InMemoryOutboxSettings>();
        settings.Should().BeEquivalentTo(new InMemoryOutboxSettings());
    }

    [Fact]
    public void WithName_ShouldSetOutboxName()
    {
        InMemoryOutboxSettingsBuilder builder = new();

        OutboxSettings settings = builder.WithName("test-outbox").Build();

        settings.As<InMemoryOutboxSettings>().OutboxName.Should().Be("test-outbox");
    }
}
