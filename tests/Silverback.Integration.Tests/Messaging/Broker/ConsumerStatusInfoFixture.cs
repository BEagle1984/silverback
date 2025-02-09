// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using Shouldly;
using Silverback.Messaging.Broker;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker;

public class ConsumerStatusInfoFixture
{
    [Fact]
    public void History_ShouldBeRolledOver()
    {
        ConsumerStatusInfo statusInfo = new();

        for (int i = 0; i < 5; i++)
        {
            statusInfo.SetStarted();
            statusInfo.SetStopped();
        }

        statusInfo.History.Count.ShouldBe(10);
        statusInfo.History.First().Status.ShouldBe(ConsumerStatus.Started);

        statusInfo.SetConnected();

        statusInfo.History.Count.ShouldBe(10);
        statusInfo.History.First().Status.ShouldBe(ConsumerStatus.Stopped);
    }
}
