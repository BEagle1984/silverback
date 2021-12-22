// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using FluentAssertions;
using Silverback.Messaging.Subscribers.Subscriptions;
using Silverback.Tests.Core.TestTypes.Subscribers;
using Xunit;

namespace Silverback.Tests.Core.Messaging.Subscribers.Subscriptions
{
    public class SubscriptionCollectionExtensionsTests
    {
        [Fact]
        public void AddTypedSubscriptionIfNotExists_NewType_Added()
        {
            var collection = new List<ISubscription>();

            collection.AddTypeSubscriptionIfNotExists(typeof(TestSubscriber), new TypeSubscriptionOptions());
            collection.AddTypeSubscriptionIfNotExists(typeof(TestCommandReplier), new TypeSubscriptionOptions());

            collection.Should().HaveCount(2);
        }

        [Fact]
        public void AddTypedSubscriptionIfNotExists_ExistingType_NotAdded()
        {
            var collection = new List<ISubscription>();

            collection.AddTypeSubscriptionIfNotExists(typeof(TestSubscriber), new TypeSubscriptionOptions());
            collection.AddTypeSubscriptionIfNotExists(typeof(TestSubscriber), new TypeSubscriptionOptions());

            collection.Should().HaveCount(1);
        }
    }
}
