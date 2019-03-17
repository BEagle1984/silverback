using System;
using System.Collections.Generic;
using System.Text;
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
