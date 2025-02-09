// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Shouldly;
using Silverback.Messaging.Subscribers.Subscriptions;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Subscribers.Subscriptions;

public class SubscriptionCollectionExtensionsTests
{
    [Fact]
    public void AddTypedSubscriptionIfNotExists_NewType_Added()
    {
        List<ISubscription> collection = [];

        collection.AddTypeSubscriptionIfNotExists(typeof(TestSubscriber), new TypeSubscriptionOptions());
        collection.AddTypeSubscriptionIfNotExists(typeof(TestOtherSubscriber), new TypeSubscriptionOptions());

        collection.Count.ShouldBe(2);
    }

    [Fact]
    public void AddTypedSubscriptionIfNotExists_ExistingType_NotAdded()
    {
        List<ISubscription> collection = [];

        collection.AddTypeSubscriptionIfNotExists(typeof(TestSubscriber), new TypeSubscriptionOptions());
        collection.AddTypeSubscriptionIfNotExists(typeof(TestSubscriber), new TypeSubscriptionOptions());

        collection.Count.ShouldBe(1);
    }

    private class TestSubscriber;

    private class TestOtherSubscriber;
}
