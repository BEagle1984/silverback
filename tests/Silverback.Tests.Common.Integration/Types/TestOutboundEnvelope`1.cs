// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Tests.Types;

internal record TestOutboundEnvelope<TMessage> : OutboundEnvelope<TMessage>
    where TMessage : class
{
    public TestOutboundEnvelope(TMessage? message, IProducer producer, ISilverbackContext? context = null)
        : base(message, producer, context)
    {
    }

    public TestOutboundEnvelope(IInboundEnvelope<TMessage> envelope, IProducer producer, ISilverbackContext? context = null)
        : base(envelope, producer, context)
    {
    }
}
