// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Core.Tests.TestTypes.Messages.Base;
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
        public async Task<string> OnRequestReceived2(IRequest<string> message)
        {
            await Task.Delay(1);
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