// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

    private readonly IPublisher _publisher;

    private readonly IMessageWrapper _messageWrapper;

    private readonly IProducerCollection _producers;

    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboundRouterBehavior" /> class.
    /// </summary>
    /// <param name="publisher">
    ///     The <see cref="IPublisher" />.
    /// </param>
    /// <param name="messageWrapper">
    ///     The <see cref="IMessageWrapper" />.
    /// </param>
    /// <param name="producers">
    ///     The <see cref="IProducerCollection" />.
    /// </param>
    public OutboundRouterBehavior(IPublisher publisher, IMessageWrapper messageWrapper, IProducerCollection producers)
    {
        _publisher = Check.NotNull(publisher, nameof(publisher));
        _messageWrapper = Check.NotNull(messageWrapper, nameof(messageWrapper));
        _producers = Check.NotNull(producers, nameof(producers));
    }

    /// <inheritdoc cref="ISorted.SortIndex" />
    public int SortIndex => IntegrationBehaviorsSortIndexes.OutboundRouter;

    /// <inheritdoc cref="IBehavior.HandleAsync" />
    public async ValueTask<IReadOnlyCollection<object?>> HandleAsync(object message, MessageHandler next)
    {
        Check.NotNull(message, nameof(message));
        Check.NotNull(next, nameof(next));

        if (message is IOutboundEnvelope envelope)
        {
            if (await ProduceEnvelopeAsync(envelope).ConfigureAwait(false))
                return [];
        }
        else if (await WrapAndRepublishRoutedMessageAsync(message).ConfigureAwait(false))
        {
            return [];
        }

        return await next(message).ConfigureAwait(false);
    }

    private static ProducerEndpoint GetProducerEndpoint(object? message, IProducer producer, SilverbackContext context) =>
        producer.EndpointConfiguration.Endpoint.GetEndpoint(message, producer.EndpointConfiguration, context.ServiceProvider);

    private static IProduceStrategyImplementation GetProduceStrategy(ProducerEndpoint endpoint, SilverbackContext context) =>
        endpoint.Configuration.Strategy.Build(context.ServiceProvider, endpoint.Configuration);

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
                    method => method.Name == "WrapAndProduceBatchAsync" &&
                              method.GetParameters().Length == 4 &&
                              method.GetParameters()[0].ParameterType.IsGenericType &&
                              method.GetParameters()[0].ParameterType.GetGenericTypeDefinition() == type))
            .MakeGenericMethod(messageType);

    private async Task<bool> ProduceEnvelopeAsync(IOutboundEnvelope envelope)
    {
        ProducerEndpoint endpoint = GetProducerEndpoint(envelope.Message, envelope.Producer, _publisher.Context);
        IProduceStrategyImplementation produceStrategy = GetProduceStrategy(endpoint, _publisher.Context);

        await produceStrategy.ProduceAsync(envelope).ConfigureAwait(false);

        return !endpoint.Configuration.EnableSubscribing;
    }

    private async ValueTask<bool> WrapAndRepublishRoutedMessageAsync(object message)
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
                        method => method.Name == "WrapAndProduceAsync" &&
                                  method.GetParameters().Length == 4)).MakeGenericMethod(args.ActualMessageType),
            (EnumerableType: enumerableType, ActualMessageType: actualMessageType));

        return await ((Task<bool>)wrapAndProduceMethod.Invoke(_messageWrapper, [message, _publisher.Context, producers, null])!).ConfigureAwait(false);
    }
}
