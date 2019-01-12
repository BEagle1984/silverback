// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Core.Tests.TestTypes.Subscribers
{
    public class TestRequestReplier : ISubscriber
    {
        public int ReceivedMessagesCount { get; private set; }

        [Subscribe]
        public string OnRequestReceived(IRequest<string> message)
        {
            ReceivedMessagesCount++;

            return "response";
        }

        [Subscribe]
        public string OnRequestReceived2(IRequest<string> message)
        {
            ReceivedMessagesCount++;

            return "response2";
        }

        [Subscribe]
        public void OnRequestReceived3(IRequest<string> message)
        {
            ReceivedMessagesCount++;
        }
    }
}