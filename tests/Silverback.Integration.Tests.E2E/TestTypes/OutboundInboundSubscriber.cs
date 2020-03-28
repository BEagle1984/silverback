// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Tests.Integration.E2E.TestTypes
{
    public class OutboundInboundSubscriber : ISubscriber
    {
        private readonly ConcurrentBag<IOutboundEnvelope> _outboundEnvelopes;
        private readonly ConcurrentBag<IInboundEnvelope> _inboundEnvelopes;

        public OutboundInboundSubscriber()
        {
            _outboundEnvelopes = new ConcurrentBag<IOutboundEnvelope>();
            _inboundEnvelopes = new ConcurrentBag<IInboundEnvelope>();
        }

        public IReadOnlyCollection<IOutboundEnvelope> OutboundEnvelopes => _outboundEnvelopes.ToList();
        public IReadOnlyCollection<IInboundEnvelope> InboundEnvelopes => _inboundEnvelopes.ToList();

        public void OnOutbound(IOutboundEnvelope envelope) => _outboundEnvelopes.Add(envelope);

        public void OnInbound(IInboundEnvelope envelope) => _inboundEnvelopes.Add(envelope);
    }
}