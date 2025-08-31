// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

internal abstract class OutboundEnvelopeFactory : IOutboundEnvelopeFactory
{
    private readonly MethodInfo _createMethod;

    private readonly ConcurrentDictionary<Type, MethodInfo> _createMethodsCache = new();

    protected OutboundEnvelopeFactory(IProducer producer)
    {
        Producer = Check.NotNull(producer, nameof(producer));

        _createMethod = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public)
                            .FirstOrDefault(methodInfo => methodInfo is { Name: nameof(Create), IsGenericMethod: true }) ??
                        throw new InvalidOperationException($"{nameof(Create)} method not found.");
    }

    protected IProducer Producer { get; }

    public IOutboundEnvelope Create(
        object? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        ISilverbackContext? context = null)
    {
        Check.NotNull(endpointConfiguration, nameof(endpointConfiguration));

        if (message == null)
            return CreateForNullMessage(headers, endpointConfiguration, context);

        MethodInfo genericMethod = _createMethodsCache.GetOrAdd(
            message.GetType(),
            static (type, createEnvelopeMethod) => createEnvelopeMethod.MakeGenericMethod(type),
            _createMethod);

        return (IOutboundEnvelope)genericMethod.Invoke(this, [message, headers, endpointConfiguration, context])!;
    }

    public abstract IOutboundEnvelope<TMessage> Create<TMessage>(
        TMessage? message,
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        ISilverbackContext? context = null)
        where TMessage : class;

    protected abstract IOutboundEnvelope CreateForNullMessage(
        IReadOnlyCollection<MessageHeader>? headers,
        ProducerEndpointConfiguration endpointConfiguration,
        ISilverbackContext? context = null);
}
