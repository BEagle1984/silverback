using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes.Messages;

namespace Silverback.Tests.TestTypes.Subscribers
{
    public class TestExceptionSubscriber : ISubscriber
    {
        [Subscribe]
        void OnMessageReceived(TestEventOne message) => throw new Exception("Test");

        [Subscribe]
        Task OnMessageReceivedAsync(TestEventTwo message) => throw new Exception("Test");
    }
}
