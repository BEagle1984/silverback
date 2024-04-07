// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EnrichedMessages;
using Silverback.Util;

namespace Silverback.Messaging.Producing.Routing;

internal sealed class OutboundEnvelopeFactory : IOutboundEnvelopeFactory
{
    private readonly IOutboundRoutingConfiguration _routingConfiguration;

    public OutboundEnvelopeFactory(IOutboundRoutingConfiguration routingConfiguration)
    {
        _routingConfiguration = routingConfiguration;
    }

    public IOutboundEnvelope CreateEnvelope(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint,
        IProducer producer,
        SilverbackContext? context = null) =>
        CreateEnvelope(message, headers, endpoint, producer, context, _routingConfiguration.PublishOutboundMessagesToInternalBus);

    internal static IOutboundEnvelope CreateSimilarEnvelope(object? message, IOutboundEnvelope originalEnvelope) =>
        CreateEnvelope(
            message,
            originalEnvelope.Headers,
            originalEnvelope.Endpoint,
            originalEnvelope.Producer,
            originalEnvelope.Context,
            originalEnvelope.AutoUnwrap);

    internal static IOutboundEnvelope CreateEnvelope(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpoint endpoint,
        IProducer producer,
        SilverbackContext? context,
        bool publishToInternalBus)
    {
        Check.NotNull(endpoint, nameof(endpoint));
        Check.NotNull(producer, nameof(producer));

        if (message is IMessageWithHeaders { Headers.Count: > 0 } messageWithHeaders)
        {
            IOutboundEnvelope outboundEnvelope = messageWithHeaders.Message == null
                ? new OutboundEnvelope(null, headers, endpoint, producer, context, publishToInternalBus)
                : (IOutboundEnvelope)Activator.CreateInstance(
                    typeof(OutboundEnvelope<>).MakeGenericType(messageWithHeaders.Message.GetType()),
                    messageWithHeaders.Message,
                    headers,
                    endpoint,
                    producer,
                    context,
                    publishToInternalBus)!;

            outboundEnvelope.Headers.AddRange(messageWithHeaders.Headers);

            return outboundEnvelope;
        }

        return message == null
            ? new OutboundEnvelope(null, headers, endpoint, producer, context, publishToInternalBus)
            : (IOutboundEnvelope)Activator.CreateInstance(
                typeof(OutboundEnvelope<>).MakeGenericType(message.GetType()),
                message,
                headers,
                endpoint,
                producer,
                context,
                publishToInternalBus)!;
    }
}
