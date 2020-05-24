// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Tests.Integration.E2E.TestTypes
{
    public class OutboundInboundSubscriber : ISubscriber
    {
        private readonly List<IOutboundEnvelope> _outboundEnvelopes = new List<IOutboundEnvelope>();

        private readonly List<IInboundEnvelope> _inboundEnvelopes = new List<IInboundEnvelope>();

        public IReadOnlyList<IOutboundEnvelope> OutboundEnvelopes => _outboundEnvelopes.ToList();

        public IReadOnlyList<IInboundEnvelope> InboundEnvelopes => _inboundEnvelopes.ToList();

        public void OnOutbound(IOutboundEnvelope envelope) => _outboundEnvelopes.Add(envelope);

        public void OnInbound(IInboundEnvelope envelope) => _inboundEnvelopes.Add(envelope);
    }
}
