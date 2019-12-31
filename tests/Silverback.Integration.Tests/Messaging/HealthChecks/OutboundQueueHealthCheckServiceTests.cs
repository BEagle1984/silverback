// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using FluentAssertions;
using NSubstitute;
using Silverback.Messaging.Connectors.Repositories;
using Silverback.Messaging.HealthChecks;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.HealthChecks
{
    public class OutboundQueueHealthCheckServiceTests
    {
        [Fact]
        public async Task CheckIsHealthy_WithoutMaxLength_LengthIsNotChecked()
        {
            var queue = Substitute.For<IOutboundQueueConsumer>();
            queue.GetLength().Returns(100);
            var service = new OutboundQueueHealthCheckService(queue);

            var result = await service.CheckIsHealthy();

            result.Should().BeTrue();
        }

        [Theory]
        [InlineData(100, true)]
        [InlineData(101, false)]
        public async Task CheckIsHealthy_WithMaxLength_LengthIsNotChecked(int currentLength, bool expected)
        {
            var queue = Substitute.For<IOutboundQueueConsumer>();
            queue.GetLength().Returns(currentLength);
            var service = new OutboundQueueHealthCheckService(queue);

            var result = await service.CheckIsHealthy(maxQueueLength: 100);

            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(30, true)]
        [InlineData(31, false)]
        public async Task CheckIsHealthy_WithDefaultMaxAge_MaxAgeIsChecked(int currentMaxAgeInSeconds, bool expected)
        {
            var queue = Substitute.For<IOutboundQueueConsumer>();
            queue.GetLength().Returns(10);
            queue.GetMaxAge().Returns(TimeSpan.FromSeconds(currentMaxAgeInSeconds));
            var service = new OutboundQueueHealthCheckService(queue);

            var result = await service.CheckIsHealthy();

            result.Should().Be(expected);
        }

        [Theory]
        [InlineData(120, true)]
        [InlineData(121, false)]
        public async Task CheckIsHealthy_WithCustomMaxAge_MaxAgeIsChecked(int currentMaxAgeInSeconds, bool expected)
        {
            var queue = Substitute.For<IOutboundQueueConsumer>();
            queue.GetLength().Returns(10);
            queue.GetMaxAge().Returns(TimeSpan.FromSeconds(currentMaxAgeInSeconds));
            var service = new OutboundQueueHealthCheckService(queue);

            var result = await service.CheckIsHealthy(TimeSpan.FromMinutes(2));

            result.Should().Be(expected);
        }
    }
}