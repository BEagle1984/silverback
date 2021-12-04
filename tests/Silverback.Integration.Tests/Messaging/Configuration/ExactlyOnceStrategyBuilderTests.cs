// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using FluentAssertions;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Inbound.ExactlyOnce;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Configuration;

public class ExactlyOnceStrategyBuilderTests
{
    [Fact]
    public void StoreOffset_OffsetStoreExactlyOnceStrategyCreated()
    {
        ExactlyOnceStrategyBuilder builder = new();

        builder.StoreOffsets();
        IExactlyOnceStrategy strategy = builder.Build();

        strategy.Should().BeOfType<OffsetStoreExactlyOnceStrategy>();
    }

    [Fact]
    public void LogMessages_LogExactlyOnceStrategyCreated()
    {
        ExactlyOnceStrategyBuilder builder = new();

        builder.LogMessages();
        IExactlyOnceStrategy strategy = builder.Build();

        strategy.Should().BeOfType<LogExactlyOnceStrategy>();
    }
}
