// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Tests.TestTypes
{
    public class TestSubscriber : ISubscriber
    {
        public int MustFailCount { get; set; }

        public int FailCount{ get; private set; }

        public List<IMessage> ReceivedMessages { get; } = new List<IMessage>();

        [Subscribe]
        void OnMessageReceived(IMessage message)
        {
            if (MustFailCount > FailCount)
            {
                FailCount++;
                throw new Exception("Test failure");
            }

            ReceivedMessages.Add(message);
        }
    }
}
