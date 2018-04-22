using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Silverback.Messaging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace Silverback.Tests.TestTypes
{
    public class TestBroker : Broker
    {
        public string ServerName { get; private set; }

        public TestBroker UseServer(string name)
        {
            ServerName = name;
            return this;
        }

        public List<byte[]> SentMessages { get; } = new List<byte[]>();

        public override Producer GetNewProducer(IEndpoint endpoint)
            => new TestProducer(this, endpoint);

        public override Consumer GetNewConsumer(IEndpoint endpoint)
            => new TestConsumer(this, endpoint);

        protected override void Connect(IEnumerable<IConsumer> consumers)
        {
            consumers.Cast<TestConsumer>().ToList().ForEach(c => c.IsReady = true);
        }

        protected override void Connect(IEnumerable<IProducer> producers)
        {
        }

        protected override void Disconnect(IEnumerable<IConsumer> consumers)
        {
        }

        protected override void Disconnect(IEnumerable<IProducer> producer)
        {
        }
    }
}
