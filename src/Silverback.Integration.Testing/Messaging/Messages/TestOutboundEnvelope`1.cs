// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Messages;

internal record TestOutboundEnvelope<TMessage> : OutboundEnvelope<TMessage>
    where TMessage : class
{
    public TestOutboundEnvelope(
        TMessage? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        IProducer producer,
        ISilverbackContext? context = null)
        : base(message, headers, endpointConfiguration, producer, context)
    {
    }
}
