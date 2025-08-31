// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Messages;

internal record KafkaOutboundEnvelope<TMessage, TKey> : KafkaOutboundEnvelope<TKey>, IKafkaOutboundEnvelope<TMessage, TKey>
    where TMessage : class
{
    public KafkaOutboundEnvelope(
        TMessage? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        IProducer producer,
        ISilverbackContext? context = null,
        IBrokerMessageIdentifier? brokerMessageIdentifier = null)
        : base(message, headers, endpointConfiguration, producer, context, brokerMessageIdentifier)
    {
        Message = message;
    }

    public new TMessage? Message { get; }

    public override Type MessageType => Message?.GetType() ?? typeof(TMessage);
}
