// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using Silverback.Messaging.Configuration;

namespace Silverback.Messaging.Messages;

public interface IOutboundEnvelopeFactory
{
    IOutboundEnvelope Create(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        ISilverbackContext? context = null);

    IOutboundEnvelope<TMessage> Create<TMessage>(
        TMessage message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        ISilverbackContext? context = null);
}
