// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Tests.TestTypes
{
    public class TestSubscriber : ISubscriber
    {
        public int MustFailCount { get; set; }

        public Func<IMessage, bool> FailCondition { get; set; }

        public int FailCount{ get; private set; }

        public List<IMessage> ReceivedMessages { get; } = new List<IMessage>();

        public TimeSpan Delay { get; set; } = TimeSpan.Zero;

        [Subscribe]
        void OnMessageReceived(IMessage message)
        {
            if (Delay > TimeSpan.Zero)
                Thread.Sleep(Delay);

            ReceivedMessages.Add(message);

            if (!(message is ISilverbackEvent) &&
                MustFailCount > FailCount || (FailCondition?.Invoke(message) ?? false))
            {
                FailCount++;
                throw new Exception("Test failure");
            }
        }
    }
}
