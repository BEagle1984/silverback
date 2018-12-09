// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.PerformanceTester.Messages;

namespace Silverback.PerformanceTester.Subscribers
{
    public class Subscriber : SubscriberBase, ISubscriber
    {
        [Subscribe]
        public void OnEventReceived(TestEvent message) => HandleMessage(message);

        [Subscribe]
        public QueryResult OnQueryReceived(IQuery<QueryResult> message)
        {
            HandleMessage(message);
            return new QueryResult();
        }
    }
}