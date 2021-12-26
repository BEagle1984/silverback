// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using FluentAssertions;
using Silverback.Messaging.Subscribers.Subscriptions;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Subscribers.Subscriptions;

public class SubscriptionCollectionExtensionsTests
{
    [Fact]
    public void AddTypedSubscriptionIfNotExists_NewType_Added()
    {
        List<ISubscription> collection = new();

        collection.AddTypeSubscriptionIfNotExists(typeof(TestSubscriber), new TypeSubscriptionOptions());
        collection.AddTypeSubscriptionIfNotExists(typeof(TestOtherSubscriber), new TypeSubscriptionOptions());

        collection.Should().HaveCount(2);
    }

    [Fact]
    public void AddTypedSubscriptionIfNotExists_ExistingType_NotAdded()
    {
        List<ISubscription> collection = new();

        collection.AddTypeSubscriptionIfNotExists(typeof(TestSubscriber), new TypeSubscriptionOptions());
        collection.AddTypeSubscriptionIfNotExists(typeof(TestSubscriber), new TypeSubscriptionOptions());

        collection.Should().HaveCount(1);
    }

    [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
    private class TestSubscriber
    {
    }

    [SuppressMessage("", "CA1812", Justification = Justifications.CalledBySilverback)]
    private class TestOtherSubscriber
    {
    }
}
