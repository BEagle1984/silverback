// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Tests.Integration.E2E.TestTypes
{
    public class SpyBrokerBehavior : IProducerBehavior, IConsumerBehavior
    {
        public IList<IOutboundEnvelope> OutboundEnvelopes { get; } = new List<IOutboundEnvelope>();

        public IList<IInboundEnvelope> InboundEnvelopes { get; } = new List<IInboundEnvelope>();

        public int SortIndex { get; } = int.MaxValue;

        public Task Handle(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            OutboundEnvelopes.Add(context.Envelope);

            return next(context);
        }

        public Task Handle(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
        {
            InboundEnvelopes.Add((IInboundEnvelope)context.Envelope);

            return next(context);
        }
    }
}
