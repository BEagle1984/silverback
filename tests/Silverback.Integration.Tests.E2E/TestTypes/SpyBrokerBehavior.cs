// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Tests.Integration.E2E.TestTypes
{
    public class SpyBrokerBehavior : IProducerBehavior, IConsumerBehavior
    {
        private readonly ConcurrentBag<IOutboundEnvelope> _outboundEnvelopes = new ConcurrentBag<IOutboundEnvelope>();

        private readonly ConcurrentBag<IInboundEnvelope> _inboundEnvelopes = new ConcurrentBag<IInboundEnvelope>();

        public IReadOnlyList<IOutboundEnvelope> OutboundEnvelopes => _outboundEnvelopes.ToList();

        public IReadOnlyList<IInboundEnvelope> InboundEnvelopes => _inboundEnvelopes.ToList();

        public int SortIndex { get; } = BrokerBehaviorsSortIndexes.Consumer.Publisher - 1;

        public Task Handle(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            _outboundEnvelopes.Add(context.Envelope);

            return next(context);
        }

        public Task Handle(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
        {
            _inboundEnvelopes.Add((IInboundEnvelope)context.Envelope);

            return next(context);
        }
    }
}
