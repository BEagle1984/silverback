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

internal abstract class InboundEnvelopeFactory : IInboundEnvelopeFactory
{
    private readonly MethodInfo _createMethod;

    private readonly MethodInfo _cloneReplacingEnvelopeMethod;

    private readonly ConcurrentDictionary<Type, MethodInfo> _createMethodsCache = new();

    private readonly ConcurrentDictionary<Type, MethodInfo> _cloneReplacingEnvelopeMethodCache = new();

    protected InboundEnvelopeFactory(IConsumer consumer)
    {
        Consumer = Check.NotNull(consumer, nameof(consumer));

        MethodInfo[] methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
        _createMethod =
            methods.FirstOrDefault(methodInfo => methodInfo is { Name: nameof(Create), IsGenericMethod: true }) ??
            throw new InvalidOperationException($"{nameof(Create)} method not found.");
        _cloneReplacingEnvelopeMethod =
            methods.FirstOrDefault(methodInfo => methodInfo is { Name: nameof(CloneReplacingMessage), IsGenericMethod: true }) ??
            throw new InvalidOperationException($"{nameof(CloneReplacingMessage)} method not found.");
    }

    protected IConsumer Consumer { get; }

    public IInboundEnvelope Create(
        object? message,
        Stream? rawMessage,
        IReadOnlyCollection<MessageHeader>? headers,
        ConsumerEndpoint endpoint,
        IBrokerMessageIdentifier brokerMessageIdentifier)
    {
        Check.NotNull(endpoint, nameof(endpoint));
        Check.NotNull(brokerMessageIdentifier, nameof(brokerMessageIdentifier));

        MethodInfo genericMethod = _createMethodsCache.GetOrAdd(
            message?.GetType() ?? typeof(object),
            static (type, method) => method.MakeGenericMethod(type),
            _createMethod);

        return (IInboundEnvelope)genericMethod.Invoke(this, [message, rawMessage, headers, endpoint, brokerMessageIdentifier])!;
    }

    public abstract IInboundEnvelope<TMessage> Create<TMessage>(
        TMessage? message,
        Stream? rawMessage,
        IReadOnlyCollection<MessageHeader>? headers,
        ConsumerEndpoint endpoint,
        IBrokerMessageIdentifier brokerMessageIdentifier)
        where TMessage : class;

    public IInboundEnvelope CloneReplacingMessage(object? message, Type messageType, IInboundEnvelope envelope)
    {
        Check.NotNull(envelope, nameof(envelope));

        MethodInfo genericMethod = _cloneReplacingEnvelopeMethodCache.GetOrAdd(
            messageType,
            static (type, method) => method.MakeGenericMethod(type),
            _cloneReplacingEnvelopeMethod);

        return (IInboundEnvelope)genericMethod.Invoke(this, [message, envelope])!;
    }

    public abstract IInboundEnvelope CloneReplacingMessage<TMessage>(TMessage? message, IInboundEnvelope envelope)
        where TMessage : class;
}
