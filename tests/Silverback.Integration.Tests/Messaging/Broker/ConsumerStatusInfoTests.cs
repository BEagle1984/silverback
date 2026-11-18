// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Shouldly;
using Silverback.Messaging.Broker;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker;

public class ConsumerStatusInfoTests
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

    [Fact]
    public void History_ShouldReturnSnapshot()
    {
        ConsumerStatusInfo statusInfo = new();
        statusInfo.SetStarted();

        IReadOnlyCollection<IConsumerStatusChange> history = statusInfo.History;
        statusInfo.SetConnected();

        history.Select(change => change.Status).ShouldBe([ConsumerStatus.Started]);
        statusInfo.History.Select(change => change.Status).ShouldBe([ConsumerStatus.Started, ConsumerStatus.Connected]);
    }

    [Theory]
    [InlineData(ConsumerStatus.Connected)]
    [InlineData(ConsumerStatus.Consuming)]
    public void RevertConnected_ShouldSetStarted_WhenConnected(ConsumerStatus status)
    {
        ConsumerStatusInfo statusInfo = new();
        statusInfo.SetStarted();
        statusInfo.SetConnected();

        if (status == ConsumerStatus.Consuming)
            statusInfo.RecordConsumedMessage(null);

        statusInfo.RevertConnected();

        statusInfo.Status.ShouldBe(ConsumerStatus.Started);
    }

    [Fact]
    public void RevertConnected_ShouldNotChangeStatus_WhenStopped()
    {
        ConsumerStatusInfo statusInfo = new();
        statusInfo.SetStarted();
        statusInfo.SetConnected();
        statusInfo.SetStopped();

        statusInfo.RevertConnected();

        statusInfo.Status.ShouldBe(ConsumerStatus.Stopped);
        statusInfo.History.Count.ShouldBe(3);
    }

    [Fact]
    public void RevertConnected_ShouldNotChangeStatus_WhenStarted()
    {
        ConsumerStatusInfo statusInfo = new();
        statusInfo.SetStarted();

        statusInfo.RevertConnected();

        statusInfo.Status.ShouldBe(ConsumerStatus.Started);
        statusInfo.History.Count.ShouldBe(1);
    }
}
