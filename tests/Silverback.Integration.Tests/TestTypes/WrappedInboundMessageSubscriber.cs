// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Subscribers;

namespace Silverback.Tests.Integration.TestTypes
{
    public class WrappedInboundMessageSubscriber : ISubscriber
    {
        public List<IInboundEnvelope<object>> ReceivedEnvelopes { get; } = new List<IInboundEnvelope<object>>();

        [Subscribe]
        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = Justifications.CalledBySilverback)]
        private void OnMessageReceived(IInboundEnvelope<object> envelope) => ReceivedEnvelopes.Add(envelope);
    }
}