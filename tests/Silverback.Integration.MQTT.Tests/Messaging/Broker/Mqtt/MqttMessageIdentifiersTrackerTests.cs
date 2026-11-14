// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Shouldly;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Mqtt;
using Xunit;

namespace Silverback.Tests.Integration.Mqtt.Messaging.Broker.Mqtt;

public class MqttMessageIdentifiersTrackerTests
{
    [Fact]
    public void GetRollbackIdentifiers_ShouldReturnSnapshot()
    {
        MqttMessageIdentifiersTracker tracker = new(new ConsumerCollection());
        tracker.TrackIdentifier(new MqttMessageIdentifier("client", "1"));

        // Read by the rollback of another sequence while the reader keeps adding messages
        IReadOnlyCollection<IBrokerMessageIdentifier> identifiers = tracker.GetRollbackIdentifiers();
        tracker.TrackIdentifier(new MqttMessageIdentifier("client", "2"));

        identifiers.ShouldBe([new MqttMessageIdentifier("client", "1")]);
        tracker.GetRollbackIdentifiers().Count.ShouldBe(2);
    }

    [Fact]
    public void GetCommitIdentifiers_ShouldReturnSnapshot()
    {
        MqttMessageIdentifiersTracker tracker = new(new ConsumerCollection());
        tracker.TrackIdentifier(new MqttMessageIdentifier("client", "1"));

        IReadOnlyCollection<IBrokerMessageIdentifier> identifiers = tracker.GetCommitIdentifiers();
        tracker.TrackIdentifier(new MqttMessageIdentifier("client", "2"));

        identifiers.ShouldBe([new MqttMessageIdentifier("client", "1")]);
    }

    [Fact]
    public void TrackIdentifier_ShouldTrackEachIdentifierOnce()
    {
        MqttMessageIdentifiersTracker tracker = new(new ConsumerCollection());

        tracker.TrackIdentifier(new MqttMessageIdentifier("client", "1"));
        tracker.TrackIdentifier(new MqttMessageIdentifier("client", "1"));

        tracker.GetRollbackIdentifiers().ShouldBe([new MqttMessageIdentifier("client", "1")]);
    }
}
