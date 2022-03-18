// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using FluentAssertions;
using Silverback.Messaging.Broker;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker
{
    public class ConsumerStatusInfoFixture
    {
        [Fact]
        public void History_ShouldBeRolledOver()
        {
            ConsumerStatusInfo statusInfo = new();

            for (int i = 0; i < 5; i++)
            {
                statusInfo.SetConnected();
                statusInfo.SetDisconnected();
            }

            statusInfo.History.Should().HaveCount(10);
            statusInfo.History.First().Status.Should().Be(ConsumerStatus.Connected);

            statusInfo.SetConnected();

            statusInfo.History.Should().HaveCount(10);
            statusInfo.History.First().Status.Should().Be(ConsumerStatus.Disconnected);
        }
    }
}
