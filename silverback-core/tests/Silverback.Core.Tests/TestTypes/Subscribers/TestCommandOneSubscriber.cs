using System;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Silverback.Messaging.Subscribers;
using Silverback.Tests.TestTypes.Domain;

namespace Silverback.Tests.TestTypes.Subscribers
{
    public class TestCommandOneSubscriber : Subscriber<TestCommandOne>
    {
        public int Handled { get; set; }

        public TestCommandOneSubscriber()
            : base(NullLoggerFactory.Instance)
        {
        }

        public override void Handle(TestCommandOne message)
        {
            Handled++;
        }
    }
}