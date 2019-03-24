// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Subscribers;

namespace Silverback.Core.Tests.TestTypes.Subscribers
{
    public abstract class GenericSubscriber<TMessage> : ISubscriber
    {
        public int ReceivedMessagesCount { get; private set; }

        [Subscribe]
        protected void OnMessageReceived(TMessage message) => ReceivedMessagesCount++;
    }
}
