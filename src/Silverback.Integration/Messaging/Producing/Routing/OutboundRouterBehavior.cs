// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Producing.Routing;

/// <summary>
///     Routes the messages to the producer by wrapping them in an <see cref="IOutboundEnvelope{TMessage}" /> that is republished to the bus.
/// </summary>
public class OutboundRouterBehavior : IBehavior, ISorted
{
    private static readonly ConcurrentDictionary<Type, MethodInfo> WrapAndProduceBatchMethods = new();

    private static readonly ConcurrentDictionary<Type, MethodInfo> SpecificWrapAndProduceMethods = new();

    private static MethodInfo? _singleMessageWrapAndProduceMethod;

    private readonly IMessageWrapper _messageWrapper;

    private readonly IProducerCollection _producers;

    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboundRouterBehavior" /> class.
    /// </summary>
    /// <param name="messageWrapper">
    ///     The <see cref="IMessageWrapper" />.
    /// </param>
    /// <param name="producers">
    ///     The <see cref="IProducerCollection" />.
    /// </param>
    public OutboundRouterBehavior(IMessageWrapper messageWrapper, IProducerCollection producers)
    {
        _messageWrapper = Check.NotNull(messageWrapper, nameof(messageWrapper));
        _producers = Check.NotNull(producers, nameof(producers));
    }

    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => IntegrationBehaviorsSortIndexes.OutboundRouter;

    /// <inheritdoc cref="IBehavior.HandleAsync" />
    public async ValueTask<IReadOnlyCollection<object?>> HandleAsync(
        IPublisher publisher,
        object message,
        MessageHandler next,
        CancellationToken cancellationToken)
    {
        Check.NotNull(message, nameof(message));
        Check.NotNull(next, nameof(next));

        return await WrapAndRepublishRoutedMessageAsync(publisher, message, cancellationToken).ConfigureAwait(false)
            ? []
            : await next(message).ConfigureAwait(false);
    }

    private static Type? GetEnumerableType(Type messageType)
    {
        if (typeof(IReadOnlyCollection<object>).IsAssignableFrom(messageType))
            return typeof(IReadOnlyCollection<>);
        if (typeof(IAsyncEnumerable<object>).IsAssignableFrom(messageType))
            return typeof(IAsyncEnumerable<>);
        if (typeof(IEnumerable<object>).IsAssignableFrom(messageType))
            return typeof(IEnumerable<>);

        return null;
    }

    private static Type GetActualMessageType(Type messageType, Type? enumerableType)
    {
        if (enumerableType == null)
            return messageType;

        return messageType.GetInterfaces().First(type => type.IsGenericType && type.GetGenericTypeDefinition() == enumerableType)
            .GetGenericArguments()[0];
    }

    private static MethodInfo GetWrapAndProduceBatchMethodInfo(Type enumerableType, Type messageType) =>
        WrapAndProduceBatchMethods.GetOrAdd(
                enumerableType,
                static type => typeof(IMessageWrapper).GetMethods().First(
                    method => method.Name == nameof(IMessageWrapper.WrapAndProduceBatchAsync) &&
                              method.GetParameters().Length == 5 &&
                              method.GetParameters()[0].ParameterType.IsGenericType &&
                              method.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == type))
            .MakeGenericMethod(messageType);

    private async ValueTask<bool> WrapAndRepublishRoutedMessageAsync(IPublisher publisher, object message, CancellationToken cancellationToken)
    {
        Type messageType = message.GetType();
        Type? enumerableType = GetEnumerableType(messageType);
        Type actualMessageType = GetActualMessageType(messageType, enumerableType);

        IReadOnlyCollection<IProducer> producers = _producers.GetProducersForMessage(actualMessageType);

        if (producers.Count == 0)
            return false;

        MethodInfo wrapAndProduceMethod = SpecificWrapAndProduceMethods.GetOrAdd(
            message.GetType(),
            static (_, args) => args.EnumerableType != null
                ? GetWrapAndProduceBatchMethodInfo(args.EnumerableType, args.ActualMessageType)
                : (_singleMessageWrapAndProduceMethod ??=
                    typeof(IMessageWrapper).GetMethods().First(
                        method => method.Name == nameof(IMessageWrapper.WrapAndProduceAsync) &&
                                  method.GetParameters().Length == 5)).MakeGenericMethod(args.ActualMessageType),
            (EnumerableType: enumerableType, ActualMessageType: actualMessageType));

        await ((Task)wrapAndProduceMethod.Invoke(_messageWrapper, [message, publisher, producers, null, cancellationToken])!).ConfigureAwait(false);

        return true;
    }
}
