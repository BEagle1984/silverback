﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Outbound.Routing;

internal sealed class OutboundEnvelopeFactory : IOutboundEnvelopeFactory
{
    private readonly IOutboundRoutingConfiguration _routingConfiguration;

    public OutboundEnvelopeFactory(IOutboundRoutingConfiguration routingConfiguration)
    {
        _routingConfiguration = routingConfiguration;
    }

    public IOutboundEnvelope CreateEnvelope(object? message, IReadOnlyCollection<MessageHeader>? headers, ProducerEndpoint endpoint) =>
        message == null
            ? new OutboundEnvelope(
                message,
                headers,
                endpoint,
                _routingConfiguration.PublishOutboundMessagesToInternalBus)
            : (IOutboundEnvelope)Activator.CreateInstance(
                typeof(OutboundEnvelope<>).MakeGenericType(message.GetType()),
                message,
                headers,
                endpoint,
                _routingConfiguration.PublishOutboundMessagesToInternalBus)!;
}
