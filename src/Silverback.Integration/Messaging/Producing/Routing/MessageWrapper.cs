// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Producing.Routing;

internal class MessageWrapper : IMessageWrapper
{
    private static MessageWrapper? _instance;

    public static MessageWrapper Instance => _instance ??= new MessageWrapper();

    public async Task<bool> WrapAndProduceAsync<TMessage>(
        TMessage? message,
        SilverbackContext context,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class
    {
        bool handled = true;

        foreach (IProducer producer in producers)
        {
            ProducerEndpoint endpoint = GetProducerEndpoint(message, producer, context);
            IProduceStrategyImplementation produceStrategy = GetProduceStrategy(endpoint, context);

            IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, context);
            envelopeConfigurationAction?.Invoke(envelope);

            await produceStrategy.ProduceAsync(envelope).ConfigureAwait(false);

            handled &= !endpoint.Configuration.EnableSubscribing;
        }

        return handled;
    }

    public async Task<bool> WrapAndProduceAsync<TMessage, TArgument>(
        TMessage? message,
        SilverbackContext context,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument actionArgument)
        where TMessage : class
    {
        bool handled = true;

        foreach (IProducer producer in producers)
        {
            ProducerEndpoint endpoint = GetProducerEndpoint(message, producer, context);
            IProduceStrategyImplementation produceStrategy = GetProduceStrategy(endpoint, context);

            IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, context);
            envelopeConfigurationAction.Invoke(envelope, actionArgument);

            await produceStrategy.ProduceAsync(envelope).ConfigureAwait(false);

            handled &= !endpoint.Configuration.EnableSubscribing;
        }

        return handled;
    }

    public async Task<bool> WrapAndProduceBatchAsync<TMessage>(
        IReadOnlyCollection<TMessage> messages,
        SilverbackContext context,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class
    {
        bool handled = true;

        foreach (IProducer producer in producers)
        {
            ProducerEndpoint endpoint = GetProducerEndpoint(messages, producer, context);
            IProduceStrategyImplementation produceStrategy = GetProduceStrategy(endpoint, context);

            await produceStrategy.ProduceAsync(
                messages.Select(
                    message =>
                    {
                        IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, context);
                        envelopeConfigurationAction?.Invoke(envelope);
                        return envelope;
                    })).ConfigureAwait(false);

            handled &= !endpoint.Configuration.EnableSubscribing;
        }

        return handled;
    }

    public async Task<bool> WrapAndProduceBatchAsync<TMessage, TArgument>(
        IReadOnlyCollection<TMessage> messages,
        SilverbackContext context,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument actionArgument)
        where TMessage : class
    {
        bool handled = true;

        foreach (IProducer producer in producers)
        {
            ProducerEndpoint endpoint = GetProducerEndpoint(messages, producer, context);
            IProduceStrategyImplementation produceStrategy = GetProduceStrategy(endpoint, context);

            await produceStrategy.ProduceAsync(
                messages.Select(
                    message =>
                    {
                        IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, context);
                        envelopeConfigurationAction.Invoke(envelope, actionArgument);
                        return envelope;
                    })).ConfigureAwait(false);

            handled &= !endpoint.Configuration.EnableSubscribing;
        }

        return handled;
    }

    public async Task<bool> WrapAndProduceBatchAsync<TMessage>(
        IEnumerable<TMessage> messages,
        SilverbackContext context,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class
    {
        if (producers.Count > 1)
        {
            throw new RoutingException(
                "Cannot route an IEnumerable batch of messages to multiple endpoints. " +
                "Please materialize into a List or array or any type implementing IReadOnlyCollection.");
        }

        IProducer producer = producers.First();

        ProducerEndpoint endpoint = GetProducerEndpoint(messages, producer, context);
        IProduceStrategyImplementation produceStrategy = GetProduceStrategy(endpoint, context);

        await produceStrategy.ProduceAsync(
            messages.Select(
                message =>
                {
                    IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, context);
                    envelopeConfigurationAction?.Invoke(envelope);
                    return envelope;
                })).ConfigureAwait(false);

        return !endpoint.Configuration.EnableSubscribing;
    }

    public async Task<bool> WrapAndProduceBatchAsync<TMessage, TArgument>(
        IEnumerable<TMessage> messages,
        SilverbackContext context,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument actionArgument)
        where TMessage : class
    {
        if (producers.Count > 1)
        {
            throw new RoutingException(
                "Cannot route an IEnumerable batch of messages to multiple endpoints. " +
                "Please materialize into a List or array or any type implementing IReadOnlyCollection.");
        }

        IProducer producer = producers.First();

        ProducerEndpoint endpoint = GetProducerEndpoint(messages, producer, context);
        IProduceStrategyImplementation produceStrategy = GetProduceStrategy(endpoint, context);

        await produceStrategy.ProduceAsync(
            messages.Select(
                message =>
                {
                    IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, context);
                    envelopeConfigurationAction.Invoke(envelope, actionArgument);
                    return envelope;
                })).ConfigureAwait(false);

        return !endpoint.Configuration.EnableSubscribing;
    }

    public async Task<bool> WrapAndProduceBatchAsync<TMessage>(
        IAsyncEnumerable<TMessage> messages,
        SilverbackContext context,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class
    {
        if (producers.Count > 1)
        {
            throw new RoutingException(
                "Cannot route an IAsyncEnumerable batch of messages to multiple endpoints. " +
                "Please materialize into a List or array or any type implementing IReadOnlyCollection.");
        }

        IProducer producer = producers.First();

        ProducerEndpoint endpoint = GetProducerEndpoint(messages, producer, context);
        IProduceStrategyImplementation produceStrategy = GetProduceStrategy(endpoint, context);

        await produceStrategy.ProduceAsync(
            messages.Select(
                message =>
                {
                    IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, context);
                    envelopeConfigurationAction?.Invoke(envelope);
                    return envelope;
                })).ConfigureAwait(false);

        return !endpoint.Configuration.EnableSubscribing;
    }

    public async Task<bool> WrapAndProduceBatchAsync<TMessage, TArgument>(
        IAsyncEnumerable<TMessage> messages,
        SilverbackContext context,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument actionArgument)
        where TMessage : class
    {
        if (producers.Count > 1)
        {
            throw new RoutingException(
                "Cannot route an IAsyncEnumerable batch of messages to multiple endpoints. " +
                "Please materialize into a List or array or any type implementing IReadOnlyCollection.");
        }

        IProducer producer = producers.First();

        ProducerEndpoint endpoint = GetProducerEndpoint(messages, producer, context);
        IProduceStrategyImplementation produceStrategy = GetProduceStrategy(endpoint, context);

        await produceStrategy.ProduceAsync(
            messages.Select(
                message =>
                {
                    IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, context);
                    envelopeConfigurationAction.Invoke(envelope, actionArgument);
                    return envelope;
                })).ConfigureAwait(false);

        return !endpoint.Configuration.EnableSubscribing;
    }

    private static IOutboundEnvelope<TMessage> CreateOutboundEnvelope<TMessage>(
        TMessage? message,
        IProducer producer,
        ProducerEndpoint endpoint,
        SilverbackContext context)
        where TMessage : class
    {
        IOutboundEnvelope<TMessage> envelope = new OutboundEnvelope<TMessage>(
            message,
            null,
            endpoint,
            producer,
            context,
            endpoint.Configuration.EnableSubscribing);

        return envelope;
    }

    private static ProducerEndpoint GetProducerEndpoint(object? message, IProducer producer, SilverbackContext context) =>
        producer.EndpointConfiguration.Endpoint.GetEndpoint(message, producer.EndpointConfiguration, context.ServiceProvider);

    private static IProduceStrategyImplementation GetProduceStrategy(ProducerEndpoint endpoint, SilverbackContext context) =>
        endpoint.Configuration.Strategy.Build(context.ServiceProvider, endpoint.Configuration);
}
