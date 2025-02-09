// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Messaging.Broker.BrokerMessageIdentifiersTracking;
using Silverback.Tests.Types;
using Xunit;

namespace Silverback.Tests.Integration.Messaging.Broker.BrokerMessageIdentifiersTracking;

public class SimpleMessageIdentifierTrackerFixture
{
    [Fact]
    public void GetCommitIdentifiers_ShouldReturnAllIdentifiers()
    {
        SimpleMessageIdentifiersTracker tracker = new();

        tracker.TrackIdentifier(new TestOffset("1", "1"));
        tracker.TrackIdentifier(new TestOffset("2", "2"));

        tracker.GetCommitIdentifiers().ShouldBe(
        [
            new TestOffset("1", "1"),
            new TestOffset("2", "2")
        ]);
    }

    [Fact]
    public void GetRollbackIdentifiers_ShouldReturnAllIdentifiers()
    {
        SimpleMessageIdentifiersTracker tracker = new();

        tracker.TrackIdentifier(new TestOffset("1", "1"));
        tracker.TrackIdentifier(new TestOffset("2", "2"));

        tracker.GetRollbackIdentifiers().ShouldBe(
        [
            new TestOffset("1", "1"),
            new TestOffset("2", "2")
        ]);
    }
}
