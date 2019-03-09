// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Core.Tests.TestTypes.Messages;
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

        // This method does nothing but it's here to ensure it doesn't break the publisher
        [Subscribe]
        public void OnRequestReceived3(IRequest<string> message)
        {
            ReceivedMessagesCount++;
        }

        // This method does nothing but it's here to ensure it doesn't break the publisher
        [Subscribe]
        public async Task OnRequestReceived4(IRequest<string> message)
        {
            await Task.Delay(1);

            ReceivedMessagesCount++;
        }

        [Subscribe]
        public async Task<IEnumerable<string>> OnRequestReceived2(TestRequestCommandTwo message)
        {
            await Task.Delay(1);
            ReceivedMessagesCount++;

            return new[] {"one", "two"};
        }

        [Subscribe]
        public async Task<IEnumerable<string>> OnRequestReceived2(TestRequestCommandThree message)
        {
            await Task.Delay(1);
            ReceivedMessagesCount++;

            return Enumerable.Empty<string>();
        }
    }
}