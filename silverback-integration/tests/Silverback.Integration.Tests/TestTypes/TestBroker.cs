using System;
using System.Collections.Generic;
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

        private readonly List<Action<byte[]>> _consumers = new List<Action<byte[]>>();


        public override Producer GetNewProducer(IEndpoint endpoint)
            => new TestProducer(this, endpoint);

        public override Consumer GetNewConsumer(IEndpoint endpoint)
            => new TestConsumer(this, endpoint, _consumers);

        protected override void Connect(IEnumerable<IConsumer> consumers)
        {
            throw new NotImplementedException();
        }

        protected override void Connect(IEnumerable<IProducer> producers)
        {
            throw new NotImplementedException();
        }

        protected override void Disconnect(IEnumerable<IConsumer> consumers)
        {
            throw new NotImplementedException();
        }

        protected override void Disconnect(IEnumerable<IProducer> producer)
        {
            throw new NotImplementedException();
        }
    }
}
