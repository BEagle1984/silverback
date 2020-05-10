// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.Core.TestTypes.Messages;
using Silverback.Tests.Core.TestTypes.Messages.Base;

namespace Silverback.Tests.Core.TestTypes.Subscribers
{
    public class TestCommandReplier : ISubscriber
    {
        public int ReceivedMessagesCount { get; private set; }

        [Subscribe]
        public string OnRequestReceived(ICommand<string> message)
        {
            ReceivedMessagesCount++;

            return "response";
        }

        [Subscribe]
        public async Task<string> OnRequestReceived2(ICommand<string> message)
        {
            await Task.Delay(1);
            ReceivedMessagesCount++;

            return "response2";
        }

        // This method does nothing but it's here to ensure it doesn't break the publisher
        [Subscribe]
        public void OnRequestReceived3(ICommand<string> message)
        {
            ReceivedMessagesCount++;
        }

        // This method does nothing but it's here to ensure it doesn't break the publisher
        [Subscribe]
        public async Task OnRequestReceived4(ICommand<string> message)
        {
            await Task.Delay(1);

            ReceivedMessagesCount++;
        }

        [Subscribe]
        public async Task<IEnumerable<string>> OnRequestReceived2(TestCommandWithReturnTwo message)
        {
            await Task.Delay(1);
            ReceivedMessagesCount++;

            return new[] { "one", "two" };
        }

        [Subscribe]
        public async Task<IEnumerable<string>> OnRequestReceived2(TestCommandWithReturnThree message)
        {
            await Task.Delay(1);
            ReceivedMessagesCount++;

            return Enumerable.Empty<string>();
        }
    }
}