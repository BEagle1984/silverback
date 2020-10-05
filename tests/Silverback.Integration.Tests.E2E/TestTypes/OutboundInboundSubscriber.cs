// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Integration.E2E.TestTypes
{
    public class OutboundInboundSubscriber
    {
        private readonly ConcurrentBag<IOutboundEnvelope> _outboundEnvelopes = new ConcurrentBag<IOutboundEnvelope>();

        private readonly ConcurrentBag<IInboundEnvelope> _inboundEnvelopes = new ConcurrentBag<IInboundEnvelope>();

        public IReadOnlyList<IOutboundEnvelope> OutboundEnvelopes => _outboundEnvelopes.ToList();

        public IReadOnlyList<IInboundEnvelope> InboundEnvelopes => _inboundEnvelopes.ToList();

        public void OnOutbound(IOutboundEnvelope envelope) => _outboundEnvelopes.Add(envelope);

        public void OnInbound(IInboundEnvelope envelope) => _inboundEnvelopes.Add(envelope);
    }
}
