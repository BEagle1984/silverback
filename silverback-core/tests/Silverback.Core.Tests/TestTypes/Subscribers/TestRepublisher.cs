using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes.Messages;

namespace Silverback.Tests.TestTypes.Subscribers
{
    public class TestRepublisher : ISubscriber
    {
        public int ReceivedMessagesCount { get; private set; }

        [Subscribe]
        public TestEventOne OnRequestReceived(TestCommandOne message)
        {
            ReceivedMessagesCount++;

            return new TestEventOne();
        }

        [Subscribe]
        public IMessage[] OnRequestReceived(TestCommandTwo message)
        {
            ReceivedMessagesCount++;

            return new IMessage[] { new TestEventOne(), new TestEventTwo() };
        }
    }
}