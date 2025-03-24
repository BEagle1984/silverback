// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using NSubstitute;
using Shouldly;
using Silverback.Messaging.HealthChecks;
using Silverback.Messaging.Producing.TransactionalOutbox;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.HealthChecks;

public class OutboxHealthCheckServiceFixture
{
    [Fact]
    public async Task CheckIsHealthy_ShouldIgnoreOutboxLength_WhenMaxLengthIsNotSet()
    {
        IOutboxReader outboxReader = Substitute.For<IOutboxReader>();
        outboxReader.GetLengthAsync().Returns(100);
        OutboxHealthCheckService service = new(outboxReader);

        bool result = await service.CheckIsHealthyAsync(TimeSpan.FromSeconds(30));

        result.ShouldBeTrue();
    }

    [Theory]
    [InlineData(100, true)]
    [InlineData(101, false)]
    public async Task CheckIsHealthy_ShouldCheckOutboxLength_WhenMaxLengthIsSet(int currentLength, bool expected)
    {
        IOutboxReader outboxReader = Substitute.For<IOutboxReader>();
        outboxReader.GetLengthAsync().Returns(currentLength);
        OutboxHealthCheckService service = new(outboxReader);

        bool result = await service.CheckIsHealthyAsync(TimeSpan.FromSeconds(30), 100);

        result.ShouldBe(expected);
    }

    [Theory]
    [InlineData(120, true)]
    [InlineData(121, false)]
    public async Task CheckIsHealthy_ShouldCheckMaxAge_WhenMaxAgeIsSet(int currentMaxAgeInSeconds, bool expected)
    {
        IOutboxReader outboxReader = Substitute.For<IOutboxReader>();
        outboxReader.GetLengthAsync().Returns(10);
        outboxReader.GetMaxAgeAsync().Returns(TimeSpan.FromSeconds(currentMaxAgeInSeconds));
        OutboxHealthCheckService service = new(outboxReader);

        bool result = await service.CheckIsHealthyAsync(TimeSpan.FromMinutes(2));

        result.ShouldBe(expected);
    }
}
