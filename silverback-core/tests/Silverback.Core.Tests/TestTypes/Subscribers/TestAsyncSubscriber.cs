using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Tests.TestTypes.Subscribers
{
    public class TestAsyncSubscriber : AsyncSubscriber<IMessage>, IDisposable
    {
        public int Handled { get; private set; }

        public bool Disposed { get; private set; }

        public TestAsyncSubscriber()
            : base(NullLoggerFactory.Instance)
        {
        }

        public override async Task HandleAsync(IMessage message)
        {
            await Task.Delay(1);
            Handled++;
        }

        public void Dispose()
        {
            Disposed = true;
        }
    }
}