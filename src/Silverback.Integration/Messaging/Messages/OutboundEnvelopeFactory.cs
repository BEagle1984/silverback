// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Linq;
using System.Reflection;
using Silverback.Messaging.Broker;
using Silverback.Util;

namespace Silverback.Messaging.Messages;

// TODO: Test this and inheritors
internal abstract class OutboundEnvelopeFactory : IOutboundEnvelopeFactory
{
    private readonly MethodInfo _createMethod;

    private readonly MethodInfo _createFromInboundEnvelopeMethod;

    private readonly MethodInfo _cloneReplacingMessageMethod;

    private readonly ConcurrentDictionary<Type, MethodInfo> _createMethodsCache = new();

    private readonly ConcurrentDictionary<Type, MethodInfo> _createFromInboundEnvelopeMethodsCache = new();

    private readonly ConcurrentDictionary<Type, MethodInfo> _cloneReplacingMessageMethodCache = new();

    protected OutboundEnvelopeFactory(IProducer producer)
    {
        Producer = Check.NotNull(producer, nameof(producer));

        MethodInfo[] methods = GetType().GetMethods(BindingFlags.Instance | BindingFlags.Public);
        _createMethod = methods.FirstOrDefault(methodInfo => methodInfo is { Name: nameof(Create), IsGenericMethod: true }) ??
                        throw new InvalidOperationException($"{nameof(Create)} method not found.");
        _createFromInboundEnvelopeMethod = methods.FirstOrDefault(methodInfo => methodInfo is { Name: nameof(CreateFromInboundEnvelope), IsGenericMethod: true }) ??
                                           throw new InvalidOperationException($"{nameof(CreateFromInboundEnvelope)} method not found.");
        _cloneReplacingMessageMethod = methods.FirstOrDefault(methodInfo => methodInfo is { Name: nameof(CloneReplacingMessage), IsGenericMethod: true }) ??
                                       throw new InvalidOperationException($"{nameof(CloneReplacingMessage)} method not found.");
    }

    protected IProducer Producer { get; }

    public IOutboundEnvelope Create(object? message, ISilverbackContext? context = null)
    {
        MethodInfo genericMethod = _createMethodsCache.GetOrAdd(
            message?.GetType() ?? typeof(object),
            static (type, createEnvelopeMethod) => createEnvelopeMethod.MakeGenericMethod(type),
            _createMethod);

        return (IOutboundEnvelope)genericMethod.Invoke(this, [message, context])!;
    }

    public IOutboundEnvelope CreateFromInboundEnvelope(IInboundEnvelope envelope, ISilverbackContext? context = null)
    {
        Check.NotNull(envelope, nameof(envelope));

        MethodInfo genericMethod = _createFromInboundEnvelopeMethodsCache.GetOrAdd(
            envelope.MessageType,
            static (type, createFromInboundEnvelopeMethod) => createFromInboundEnvelopeMethod.MakeGenericMethod(type),
            _createFromInboundEnvelopeMethod);

        return (IOutboundEnvelope)genericMethod.Invoke(this, [envelope, context])!;
    }

    public IOutboundEnvelope CloneReplacingMessage(object? message, Type messageType, IOutboundEnvelope envelope)
    {
        Check.NotNull(envelope, nameof(envelope));

        MethodInfo genericMethod = _cloneReplacingMessageMethodCache.GetOrAdd(
            messageType,
            static (type, method) => method.MakeGenericMethod(type),
            _cloneReplacingMessageMethod);

        return (IOutboundEnvelope)genericMethod.Invoke(this, [message, envelope])!;
    }

    public abstract IOutboundEnvelope<TMessage> Create<TMessage>(TMessage? message, ISilverbackContext? context = null)
        where TMessage : class;

    public abstract IOutboundEnvelope<TMessage> CreateFromInboundEnvelope<TMessage>(
        IInboundEnvelope<TMessage> envelope,
        ISilverbackContext? context = null)
        where TMessage : class;

    public abstract IOutboundEnvelope CloneReplacingMessage<TMessage>(TMessage? message, IOutboundEnvelope envelope)
        where TMessage : class;
}
