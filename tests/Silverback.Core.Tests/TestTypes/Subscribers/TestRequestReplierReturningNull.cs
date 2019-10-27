// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.TestTypes.Messages.Base;

namespace Silverback.Tests.Core.TestTypes.Subscribers
{
    public class TestRequestReplierReturningNull : ISubscriber
    {
        public int ReceivedMessagesCount { get; private set; }

        [Subscribe]
        public string OnRequestReceived(IRequest<string> message)
        {
            ReceivedMessagesCount++;

            return null;
        }


        [Subscribe]
        public async Task<string> OnRequestReceived2(IRequest<string> message)
        {
            await Task.Delay(1);
            ReceivedMessagesCount++;

            return null;
        }
    }
}