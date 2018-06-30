using System;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Tests.TestTypes.Subscribers
{
    public class TestSubscriber : Subscriber<IMessage>, IDisposable
    {
        public int Handled { get; private set; }
        public bool Disposed { get; private set; }

        public override void Handle(IMessage message)
        {
            Handled++;
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}
