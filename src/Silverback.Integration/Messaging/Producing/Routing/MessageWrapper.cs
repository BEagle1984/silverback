// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Producing.Routing;

internal class MessageWrapper : IMessageWrapper
{
    private static MessageWrapper? _instance;

    public static MessageWrapper Instance => _instance ??= new MessageWrapper();

    public async Task WrapAndProduceAsync<TMessage>(
        TMessage? message,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        foreach (IProducer producer in producers)
        {
            ProducerEndpoint endpoint = GetProducerEndpoint(message, producer, publisher.Context);
            IProduceStrategyImplementation produceStrategy = GetProduceStrategy(endpoint, publisher.Context);

            IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
            envelopeConfigurationAction?.Invoke(envelope);

            if (endpoint.Configuration.EnableSubscribing)
                await publisher.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);

            await produceStrategy.ProduceAsync(envelope, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task WrapAndProduceAsync<TMessage, TArgument>(
        TMessage? message,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        foreach (IProducer producer in producers)
        {
            ProducerEndpoint endpoint = GetProducerEndpoint(message, producer, publisher.Context);
            IProduceStrategyImplementation produceStrategy = GetProduceStrategy(endpoint, publisher.Context);

            IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
            envelopeConfigurationAction.Invoke(envelope, argument);

            if (endpoint.Configuration.EnableSubscribing)
                await publisher.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);

            await produceStrategy.ProduceAsync(envelope, cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task WrapAndProduceBatchAsync<TMessage>(
        IReadOnlyCollection<TMessage?> messages,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        foreach (IProducer producer in producers)
        {
            ProducerEndpoint endpoint = GetProducerEndpoint(messages, producer, publisher.Context);
            IProduceStrategyImplementation produceStrategy = GetProduceStrategy(endpoint, publisher.Context);

            if (endpoint.Configuration.EnableSubscribing)
            {
                await produceStrategy.ProduceAsync(
                    messages.ToAsyncEnumerable().SelectAwait(
                        async message =>
                        {
                            IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
                            envelopeConfigurationAction?.Invoke(envelope);

                            await publisher.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);

                            return envelope;
                        }),
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await produceStrategy.ProduceAsync(
                    messages.Select(
                        message =>
                        {
                            IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
                            envelopeConfigurationAction?.Invoke(envelope);
                            return envelope;
                        }),
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task WrapAndProduceBatchAsync<TMessage, TArgument>(
        IReadOnlyCollection<TMessage?> messages,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        foreach (IProducer producer in producers)
        {
            ProducerEndpoint endpoint = GetProducerEndpoint(messages, producer, publisher.Context);
            IProduceStrategyImplementation produceStrategy = GetProduceStrategy(endpoint, publisher.Context);

            if (endpoint.Configuration.EnableSubscribing)
            {
                await produceStrategy.ProduceAsync(
                    messages.ToAsyncEnumerable().SelectAwait(
                        async message =>
                        {
                            IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
                            envelopeConfigurationAction.Invoke(envelope, argument);

                            await publisher.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);

                            return envelope;
                        }),
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await produceStrategy.ProduceAsync(
                    messages.Select(
                        message =>
                        {
                            IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
                            envelopeConfigurationAction.Invoke(envelope, argument);
                            return envelope;
                        }),
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task WrapAndProduceBatchAsync<TSource, TMessage>(
        IReadOnlyCollection<TSource> sources,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Func<TSource, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource>? envelopeConfigurationAction = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        foreach (IProducer producer in producers)
        {
            ProducerEndpoint endpoint = GetProducerEndpoint(sources, producer, publisher.Context);
            IProduceStrategyImplementation produceStrategy = GetProduceStrategy(endpoint, publisher.Context);

            if (endpoint.Configuration.EnableSubscribing)
            {
                await produceStrategy.ProduceAsync(
                    sources.ToAsyncEnumerable().SelectAwait(
                        async source =>
                        {
                            TMessage? message = mapperFunction.Invoke(source);
                            IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
                            envelopeConfigurationAction?.Invoke(envelope, source);

                            await publisher.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);

                            return envelope;
                        }),
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await produceStrategy.ProduceAsync(
                    sources.Select(
                        source =>
                        {
                            TMessage? message = mapperFunction.Invoke(source);
                            IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
                            envelopeConfigurationAction?.Invoke(envelope, source);
                            return envelope;
                        }),
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task WrapAndProduceBatchAsync<TSource, TMessage, TArgument>(
        IReadOnlyCollection<TSource> sources,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Func<TSource, TArgument, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        foreach (IProducer producer in producers)
        {
            ProducerEndpoint endpoint = GetProducerEndpoint(sources, producer, publisher.Context);
            IProduceStrategyImplementation produceStrategy = GetProduceStrategy(endpoint, publisher.Context);

            if (endpoint.Configuration.EnableSubscribing)
            {
                await produceStrategy.ProduceAsync(
                    sources.ToAsyncEnumerable().SelectAwait(
                        async source =>
                        {
                            TMessage? message = mapperFunction.Invoke(source, argument);
                            IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
                            envelopeConfigurationAction.Invoke(envelope, source, argument);

                            await publisher.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);

                            return envelope;
                        }),
                    cancellationToken).ConfigureAwait(false);
            }
            else
            {
                await produceStrategy.ProduceAsync(
                    sources.Select(
                        source =>
                        {
                            TMessage? message = mapperFunction.Invoke(source, argument);
                            IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
                            envelopeConfigurationAction.Invoke(envelope, source, argument);
                            return envelope;
                        }),
                    cancellationToken).ConfigureAwait(false);
            }
        }
    }

    public async Task WrapAndProduceBatchAsync<TMessage>(
        IEnumerable<TMessage?> messages,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (producers.Count > 1)
        {
            throw new RoutingException(
                "Cannot route an IEnumerable batch of messages to multiple endpoints. " +
                "Please materialize into a List or an array or any type implementing IReadOnlyCollection.");
        }

        IProducer producer = producers.First();

        ProducerEndpoint endpoint = GetProducerEndpoint(messages, producer, publisher.Context);
        IProduceStrategyImplementation produceStrategy = GetProduceStrategy(endpoint, publisher.Context);

        if (endpoint.Configuration.EnableSubscribing)
        {
            await produceStrategy.ProduceAsync(
                messages.ToAsyncEnumerable().SelectAwait(
                    async message =>
                    {
                        IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
                        envelopeConfigurationAction?.Invoke(envelope);

                        await publisher.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);

                        return envelope;
                    }),
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await produceStrategy.ProduceAsync(
                messages.Select(
                    message =>
                    {
                        IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
                        envelopeConfigurationAction?.Invoke(envelope);
                        return envelope;
                    }),
                cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task WrapAndProduceBatchAsync<TMessage, TArgument>(
        IEnumerable<TMessage?> messages,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (producers.Count > 1)
        {
            throw new RoutingException(
                "Cannot route an IEnumerable batch of messages to multiple endpoints. " +
                "Please materialize into a List or an array or any type implementing IReadOnlyCollection.");
        }

        IProducer producer = producers.First();

        ProducerEndpoint endpoint = GetProducerEndpoint(messages, producer, publisher.Context);
        IProduceStrategyImplementation produceStrategy = GetProduceStrategy(endpoint, publisher.Context);

        if (endpoint.Configuration.EnableSubscribing)
        {
            await produceStrategy.ProduceAsync(
                messages.ToAsyncEnumerable().SelectAwait(
                    async message =>
                    {
                        IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
                        envelopeConfigurationAction.Invoke(envelope, argument);

                        await publisher.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);

                        return envelope;
                    }),
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await produceStrategy.ProduceAsync(
                messages.Select(
                    message =>
                    {
                        IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
                        envelopeConfigurationAction.Invoke(envelope, argument);
                        return envelope;
                    }),
                cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task WrapAndProduceBatchAsync<TSource, TMessage>(
        IEnumerable<TSource> sources,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Func<TSource, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource>? envelopeConfigurationAction = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (producers.Count > 1)
        {
            throw new RoutingException(
                "Cannot route an IEnumerable batch of messages to multiple endpoints. " +
                "Please materialize into a List or an array or any type implementing IReadOnlyCollection.");
        }

        IProducer producer = producers.First();

        ProducerEndpoint endpoint = GetProducerEndpoint(sources, producer, publisher.Context);
        IProduceStrategyImplementation produceStrategy = GetProduceStrategy(endpoint, publisher.Context);

        if (endpoint.Configuration.EnableSubscribing)
        {
            await produceStrategy.ProduceAsync(
                sources.ToAsyncEnumerable().SelectAwait(
                    async source =>
                    {
                        TMessage? message = mapperFunction.Invoke(source);
                        IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
                        envelopeConfigurationAction?.Invoke(envelope, source);

                        await publisher.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);

                        return envelope;
                    }),
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await produceStrategy.ProduceAsync(
                sources.Select(
                    source =>
                    {
                        TMessage? message = mapperFunction.Invoke(source);
                        IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
                        envelopeConfigurationAction?.Invoke(envelope, source);
                        return envelope;
                    }),
                cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task WrapAndProduceBatchAsync<TSource, TMessage, TArgument>(
        IEnumerable<TSource> sources,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Func<TSource, TArgument, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (producers.Count > 1)
        {
            throw new RoutingException(
                "Cannot route an IEnumerable batch of messages to multiple endpoints. " +
                "Please materialize into a List or an array or any type implementing IReadOnlyCollection.");
        }

        IProducer producer = producers.First();

        ProducerEndpoint endpoint = GetProducerEndpoint(sources, producer, publisher.Context);
        IProduceStrategyImplementation produceStrategy = GetProduceStrategy(endpoint, publisher.Context);

        if (endpoint.Configuration.EnableSubscribing)
        {
            await produceStrategy.ProduceAsync(
                sources.ToAsyncEnumerable().SelectAwait(
                    async source =>
                    {
                        TMessage? message = mapperFunction.Invoke(source, argument);
                        IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
                        envelopeConfigurationAction.Invoke(envelope, source, argument);

                        await publisher.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);

                        return envelope;
                    }),
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await produceStrategy.ProduceAsync(
                sources.Select(
                    source =>
                    {
                        TMessage? message = mapperFunction.Invoke(source, argument);
                        IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
                        envelopeConfigurationAction.Invoke(envelope, source, argument);
                        return envelope;
                    }),
                cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task WrapAndProduceBatchAsync<TMessage>(
        IAsyncEnumerable<TMessage?> messages,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (producers.Count > 1)
        {
            throw new RoutingException(
                "Cannot route an IAsyncEnumerable batch of messages to multiple endpoints. " +
                "Please materialize into a List or an array or any type implementing IReadOnlyCollection.");
        }

        IProducer producer = producers.First();

        ProducerEndpoint endpoint = GetProducerEndpoint(messages, producer, publisher.Context);
        IProduceStrategyImplementation produceStrategy = GetProduceStrategy(endpoint, publisher.Context);

        if (endpoint.Configuration.EnableSubscribing)
        {
            await produceStrategy.ProduceAsync(
                messages.SelectAwait(
                    async message =>
                    {
                        IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
                        envelopeConfigurationAction?.Invoke(envelope);

                        await publisher.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);

                        return envelope;
                    }),
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await produceStrategy.ProduceAsync(
                messages.Select(
                    message =>
                    {
                        IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
                        envelopeConfigurationAction?.Invoke(envelope);
                        return envelope;
                    }),
                cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task WrapAndProduceBatchAsync<TMessage, TArgument>(
        IAsyncEnumerable<TMessage?> messages,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (producers.Count > 1)
        {
            throw new RoutingException(
                "Cannot route an IAsyncEnumerable batch of messages to multiple endpoints. " +
                "Please materialize into a List or an array or any type implementing IReadOnlyCollection.");
        }

        IProducer producer = producers.First();

        ProducerEndpoint endpoint = GetProducerEndpoint(messages, producer, publisher.Context);
        IProduceStrategyImplementation produceStrategy = GetProduceStrategy(endpoint, publisher.Context);

        if (endpoint.Configuration.EnableSubscribing)
        {
            await produceStrategy.ProduceAsync(
                messages.SelectAwait(
                    async message =>
                    {
                        IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
                        envelopeConfigurationAction.Invoke(envelope, argument);

                        await publisher.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);

                        return envelope;
                    }),
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await produceStrategy.ProduceAsync(
                messages.Select(
                    message =>
                    {
                        IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
                        envelopeConfigurationAction.Invoke(envelope, argument);
                        return envelope;
                    }),
                cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task WrapAndProduceBatchAsync<TSource, TMessage>(
        IAsyncEnumerable<TSource> sources,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Func<TSource, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource>? envelopeConfigurationAction = null,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (producers.Count > 1)
        {
            throw new RoutingException(
                "Cannot route an IAsyncEnumerable batch of messages to multiple endpoints. " +
                "Please materialize into a List or an array or any type implementing IReadOnlyCollection.");
        }

        IProducer producer = producers.First();

        ProducerEndpoint endpoint = GetProducerEndpoint(sources, producer, publisher.Context);
        IProduceStrategyImplementation produceStrategy = GetProduceStrategy(endpoint, publisher.Context);

        if (endpoint.Configuration.EnableSubscribing)
        {
            await produceStrategy.ProduceAsync(
                sources.SelectAwait(
                    async source =>
                    {
                        TMessage? message = mapperFunction.Invoke(source);
                        IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
                        envelopeConfigurationAction?.Invoke(envelope, source);

                        await publisher.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);

                        return envelope;
                    }),
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await produceStrategy.ProduceAsync(
                sources.Select(
                    source =>
                    {
                        TMessage? message = mapperFunction.Invoke(source);
                        IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
                        envelopeConfigurationAction?.Invoke(envelope, source);
                        return envelope;
                    }),
                cancellationToken).ConfigureAwait(false);
        }
    }

    public async Task WrapAndProduceBatchAsync<TSource, TMessage, TArgument>(
        IAsyncEnumerable<TSource> sources,
        IPublisher publisher,
        IReadOnlyCollection<IProducer> producers,
        Func<TSource, TArgument, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class
    {
        if (producers.Count > 1)
        {
            throw new RoutingException(
                "Cannot route an IAsyncEnumerable batch of messages to multiple endpoints. " +
                "Please materialize into a List or an array or any type implementing IReadOnlyCollection.");
        }

        IProducer producer = producers.First();

        ProducerEndpoint endpoint = GetProducerEndpoint(sources, producer, publisher.Context);
        IProduceStrategyImplementation produceStrategy = GetProduceStrategy(endpoint, publisher.Context);

        if (endpoint.Configuration.EnableSubscribing)
        {
            await produceStrategy.ProduceAsync(
                sources.SelectAwait(
                    async source =>
                    {
                        TMessage? message = mapperFunction.Invoke(source, argument);
                        IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
                        envelopeConfigurationAction.Invoke(envelope, source, argument);

                        await publisher.PublishAsync(envelope, cancellationToken).ConfigureAwait(false);

                        return envelope;
                    }),
                cancellationToken).ConfigureAwait(false);
        }
        else
        {
            await produceStrategy.ProduceAsync(
                sources.Select(
                    source =>
                    {
                        TMessage? message = mapperFunction.Invoke(source, argument);
                        IOutboundEnvelope<TMessage> envelope = CreateOutboundEnvelope(message, producer, endpoint, publisher.Context);
                        envelopeConfigurationAction.Invoke(envelope, source, argument);
                        return envelope;
                    }),
                cancellationToken).ConfigureAwait(false);
        }
    }

    private static OutboundEnvelope<TMessage> CreateOutboundEnvelope<TMessage>(
        TMessage? message,
        IProducer producer,
        ProducerEndpoint endpoint,
        ISilverbackContext context)
        where TMessage : class =>
        new(message, null, endpoint, producer, context);

    private static ProducerEndpoint GetProducerEndpoint(object? message, IProducer producer, ISilverbackContext context) =>
        producer.EndpointConfiguration.Endpoint.GetEndpoint(message, producer.EndpointConfiguration, context.ServiceProvider);

    private static IProduceStrategyImplementation GetProduceStrategy(ProducerEndpoint endpoint, ISilverbackContext context) =>
        endpoint.Configuration.Strategy.Build(context.ServiceProvider, endpoint.Configuration);
}
