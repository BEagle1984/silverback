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

        public override IProducer GetProducer(IEndpoint endpoint)
            => new TestProducer(this, endpoint);

        public override IConsumer GetConsumer(IEndpoint endpoint)
            => new TestConsumer(this, endpoint, _consumers);

        public override void Connect(IEndpoint[] inboundEndpoints, IEndpoint[] outboundEndpoints)
        {
            throw new NotImplementedException();
        }
    }
}
