// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.E2E.TestTypes
{
    public class SpyBrokerBehavior : IProducerBehavior, IConsumerBehavior
    {
        private readonly List<IOutboundEnvelope> _outboundEnvelopes = new List<IOutboundEnvelope>();

        private readonly List<IInboundEnvelope> _inboundEnvelopes = new List<IInboundEnvelope>();

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "Lock on writes only")]
        public IReadOnlyList<IOutboundEnvelope> OutboundEnvelopes => _outboundEnvelopes.ToList();

        [SuppressMessage("ReSharper", "InconsistentlySynchronizedField", Justification = "Lock on writes only")]
        public IReadOnlyList<IInboundEnvelope> InboundEnvelopes => _inboundEnvelopes.ToList();

        public int SortIndex { get; } = BrokerBehaviorsSortIndexes.Consumer.Publisher - 1;

        public Task HandleAsync(ProducerPipelineContext context, ProducerBehaviorHandler next)
        {
            lock (_outboundEnvelopes)
            {
                _outboundEnvelopes.Add(context.Envelope);
            }

            return next(context);
        }

        public Task HandleAsync(
            ConsumerPipelineContext context,
            ConsumerBehaviorHandler next)
        {
            lock (_inboundEnvelopes)
            {
                _inboundEnvelopes.Add((IInboundEnvelope)context.Envelope);
            }

            return next(context);
        }
    }
}
