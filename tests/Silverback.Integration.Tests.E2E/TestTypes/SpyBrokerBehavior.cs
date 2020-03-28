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
    public class SpyBrokerBehavior : IProducerBehavior, IConsumerBehavior, ISorted
    {
        private readonly ConcurrentBag<IOutboundEnvelope> _outboundEnvelopes;
        private readonly ConcurrentBag<IInboundEnvelope> _inboundEnvelopes;

        public SpyBrokerBehavior()
        {
            _outboundEnvelopes = new ConcurrentBag<IOutboundEnvelope>();
            _inboundEnvelopes = new ConcurrentBag<IInboundEnvelope>();
        }

        public IReadOnlyCollection<IOutboundEnvelope> OutboundEnvelopes => _outboundEnvelopes.ToList();
        public IReadOnlyCollection<IInboundEnvelope> InboundEnvelopes => _inboundEnvelopes.ToList();

        public Task Handle(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            _outboundEnvelopes.Add(context.Envelope);

            return next(context);
        }

        public Task Handle(
            ConsumerPipelineContext context,
            IServiceProvider serviceProvider,
            ConsumerBehaviorHandler next)
        {
            context.Envelopes.ForEach(envelope => _inboundEnvelopes.Add((IInboundEnvelope) envelope));

            return next(context, serviceProvider);
        }

        public int SortIndex { get; } = int.MaxValue;
    }
}