// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Threading.Tasks;
using NSubstitute;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.TestTypes
{
    public class TestOtherProducer : Producer<TestOtherBroker, TestOtherProducerEndpoint>
    {
        public TestOtherProducer(
            TestOtherBroker broker,
            TestOtherProducerEndpoint endpoint,
            IReadOnlyCollection<IProducerBehavior>? behaviors)
            : base(
                broker,
                endpoint,
                behaviors,
                Substitute.For<ISilverbackIntegrationLogger<TestOtherProducer>>())
        {
            ProducedMessages = broker.ProducedMessages;
        }

        public List<ProducedMessage> ProducedMessages { get; }

        protected override IOffset? ProduceCore(IOutboundEnvelope envelope)
        {
            ProducedMessages.Add(new ProducedMessage(envelope.RawMessage, envelope.Headers, Endpoint));
            return null;
        }

        protected override Task<IOffset?> ProduceAsyncCore(IOutboundEnvelope envelope)
        {
            Produce(envelope.RawMessage, envelope.Headers);
            return Task.FromResult<IOffset?>(null);
        }
    }
}
