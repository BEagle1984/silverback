// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Messages;

namespace Silverback.PerformanceTester.Subscribers
{
    public abstract class SubscriberBase
    {
        public event EventHandler<IMessage> Received;

        public int ReceivedMessagesCount { get; private set; }

        protected void HandleMessage(IMessage message)
        {
            Received?.Invoke(this, message);
            ReceivedMessagesCount++;
        }
    }
}