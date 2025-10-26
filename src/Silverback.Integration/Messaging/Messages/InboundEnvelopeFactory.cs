// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

// TODO: Test this and inheritors
internal abstract class InboundEnvelopeFactory : IInboundEnvelopeFactory
{
    private readonly MethodInfo _cloneReplacingMessageMethod;

    private readonly ConcurrentDictionary<Type, MethodInfo> _cloneReplacingMessageMethodCache = new();

    protected InboundEnvelopeFactory(IConsumer consumer)
    {
        Consumer = Check.NotNull(consumer, nameof(consumer));

        MethodInfo[] methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
        _cloneReplacingMessageMethod = methods.FirstOrDefault(methodInfo => methodInfo is { Name: nameof(CloneReplacingMessage), IsGenericMethod: true }) ??
                                       throw new InvalidOperationException($"{nameof(CloneReplacingMessage)} method not found.");
    }

    protected IConsumer Consumer { get; }

    public IInboundEnvelope Create(Stream? rawMessage, ConsumerEndpoint endpoint, IBrokerMessageIdentifier brokerMessageIdentifier)
    {
        Check.NotNull(endpoint, nameof(endpoint));
        Check.NotNull(brokerMessageIdentifier, nameof(brokerMessageIdentifier));

        return Create<object>(null, rawMessage, endpoint, brokerMessageIdentifier);
    }

    public IInboundEnvelope<TMessage> Create<TMessage>(Stream? rawMessage, ConsumerEndpoint endpoint, IBrokerMessageIdentifier brokerMessageIdentifier) where TMessage : class => throw new NotImplementedException();

    public abstract IInboundEnvelope<TMessage> Create<TMessage>(
        TMessage? message,
        Stream? rawMessage,
        ConsumerEndpoint endpoint,
        IBrokerMessageIdentifier brokerMessageIdentifier)
        where TMessage : class;

    public IInboundEnvelope CloneReplacingMessage(object? message, Type messageType, IInboundEnvelope envelope)
    {
        Check.NotNull(envelope, nameof(envelope));

        MethodInfo genericMethod = _cloneReplacingMessageMethodCache.GetOrAdd(
            messageType,
            static (type, method) => method.MakeGenericMethod(type),
            _cloneReplacingMessageMethod);

        return (IInboundEnvelope)genericMethod.Invoke(this, [message, envelope])!;
    }

    public abstract IInboundEnvelope CloneReplacingMessage<TMessage>(TMessage? message, IInboundEnvelope envelope)
        where TMessage : class;
}
