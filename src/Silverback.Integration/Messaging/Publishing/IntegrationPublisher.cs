// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Publishing;

internal partial class IntegrationPublisher : IIntegrationPublisher
{
    private readonly IPublisher _publisher;

    public IntegrationPublisher(IPublisher publisher)
    {
        _publisher = Check.NotNull(publisher, nameof(publisher));
    }

    public Task WrapAndPublishBatchAsync<TMessage>(IReadOnlyCollection<TMessage?> messages, CancellationToken cancellationToken = default)
        where TMessage : class =>
        _publisher.WrapAndPublishBatchAsync(messages, cancellationToken);

    public Task WrapAndPublishBatchAsync<TMessage>(
        IReadOnlyCollection<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        _publisher.WrapAndPublishBatchAsync(messages, envelopeConfigurationAction, cancellationToken);

    public Task WrapAndPublishBatchAsync<TMessage, TArgument>(
        IReadOnlyCollection<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        _publisher.WrapAndPublishBatchAsync(messages, envelopeConfigurationAction, argument, cancellationToken);

    public Task WrapAndPublishBatchAsync<TSource, TMessage>(
        IReadOnlyCollection<TSource> sources,
        Func<TSource, TMessage?> mapperFunction,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        _publisher.WrapAndPublishBatchAsync(sources, mapperFunction, cancellationToken);

    public Task WrapAndPublishBatchAsync<TSource, TMessage>(
        IReadOnlyCollection<TSource> sources,
        Func<TSource, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource>? envelopeConfigurationAction,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        _publisher.WrapAndPublishBatchAsync(sources, mapperFunction, envelopeConfigurationAction, cancellationToken);

    public Task WrapAndPublishBatchAsync<TSource, TMessage, TArgument>(
        IReadOnlyCollection<TSource> sources,
        Func<TSource, TArgument, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        _publisher.WrapAndPublishBatchAsync(sources, mapperFunction, envelopeConfigurationAction, argument, cancellationToken);

    public Task WrapAndPublishBatchAsync<TMessage>(IEnumerable<TMessage?> messages, CancellationToken cancellationToken = default)
        where TMessage : class =>
        _publisher.WrapAndPublishBatchAsync(messages, cancellationToken);

    public Task WrapAndPublishBatchAsync<TMessage>(
        IEnumerable<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        _publisher.WrapAndPublishBatchAsync(messages, envelopeConfigurationAction, cancellationToken);

    public Task WrapAndPublishBatchAsync<TMessage, TArgument>(
        IEnumerable<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        _publisher.WrapAndPublishBatchAsync(messages, envelopeConfigurationAction, argument, cancellationToken);

    public Task WrapAndPublishBatchAsync<TSource, TMessage>(
        IEnumerable<TSource> sources,
        Func<TSource, TMessage?> mapperFunction,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        _publisher.WrapAndPublishBatchAsync(sources, mapperFunction, cancellationToken);

    public Task WrapAndPublishBatchAsync<TSource, TMessage>(
        IEnumerable<TSource> sources,
        Func<TSource, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource>? envelopeConfigurationAction,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        _publisher.WrapAndPublishBatchAsync(sources, mapperFunction, envelopeConfigurationAction, cancellationToken);

    public Task WrapAndPublishBatchAsync<TSource, TMessage, TArgument>(
        IEnumerable<TSource> sources,
        Func<TSource, TArgument, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        _publisher.WrapAndPublishBatchAsync(sources, mapperFunction, envelopeConfigurationAction, argument, cancellationToken);

    public Task WrapAndPublishBatchAsync<TMessage>(IAsyncEnumerable<TMessage?> messages, CancellationToken cancellationToken = default)
        where TMessage : class =>
        _publisher.WrapAndPublishBatchAsync(messages, cancellationToken);

    public Task WrapAndPublishBatchAsync<TMessage>(
        IAsyncEnumerable<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        _publisher.WrapAndPublishBatchAsync(messages, envelopeConfigurationAction, cancellationToken);

    public Task WrapAndPublishBatchAsync<TMessage, TArgument>(
        IAsyncEnumerable<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        _publisher.WrapAndPublishBatchAsync(messages, envelopeConfigurationAction, argument, cancellationToken);

    public Task WrapAndPublishBatchAsync<TSource, TMessage>(
        IAsyncEnumerable<TSource> sources,
        Func<TSource, TMessage?> mapperFunction,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        _publisher.WrapAndPublishBatchAsync(sources, mapperFunction, cancellationToken);

    public Task WrapAndPublishBatchAsync<TSource, TMessage>(
        IAsyncEnumerable<TSource> sources,
        Func<TSource, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource>? envelopeConfigurationAction,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        _publisher.WrapAndPublishBatchAsync(sources, mapperFunction, envelopeConfigurationAction, cancellationToken);

    public Task WrapAndPublishBatchAsync<TSource, TMessage, TArgument>(
        IAsyncEnumerable<TSource> sources,
        Func<TSource, TArgument, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        _publisher.WrapAndPublishBatchAsync(sources, mapperFunction, envelopeConfigurationAction, argument, cancellationToken);
}
