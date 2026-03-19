// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Publishing;

internal partial class IntegrationPublisher
{
    public void WrapAndPublish<TMessage>(TMessage? message, Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class =>
        _publisher.WrapAndPublish(message, envelopeConfigurationAction);

    public void WrapAndPublish<TMessage, TArgument>(
        TMessage? message,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument)
        where TMessage : class =>
        _publisher.WrapAndPublish(message, envelopeConfigurationAction, argument);

    public void WrapAndPublishBatch<TMessage>(
        IReadOnlyCollection<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class =>
        _publisher.WrapAndPublishBatch(messages, envelopeConfigurationAction);

    public void WrapAndPublishBatch<TMessage, TArgument>(
        IReadOnlyCollection<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument)
        where TMessage : class =>
        _publisher.WrapAndPublishBatch(messages, envelopeConfigurationAction, argument);

    public void WrapAndPublishBatch<TSource, TMessage>(
        IReadOnlyCollection<TSource> sources,
        Func<TSource, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource>? envelopeConfigurationAction = null)
        where TMessage : class =>
        _publisher.WrapAndPublishBatch(sources, mapperFunction, envelopeConfigurationAction);

    public void WrapAndPublishBatch<TSource, TMessage, TArgument>(
        IReadOnlyCollection<TSource> sources,
        Func<TSource, TArgument, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource, TArgument> envelopeConfigurationAction,
        TArgument argument)
        where TMessage : class =>
        _publisher.WrapAndPublishBatch(sources, mapperFunction, envelopeConfigurationAction, argument);

    public void WrapAndPublishBatch<TMessage>(
        IEnumerable<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class =>
        _publisher.WrapAndPublishBatch(messages, envelopeConfigurationAction);

    public void WrapAndPublishBatch<TMessage, TArgument>(
        IEnumerable<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument)
        where TMessage : class =>
        _publisher.WrapAndPublishBatch(messages, envelopeConfigurationAction, argument);

    public void WrapAndPublishBatch<TSource, TMessage>(
        IEnumerable<TSource> sources,
        Func<TSource, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource>? envelopeConfigurationAction = null)
        where TMessage : class =>
        _publisher.WrapAndPublishBatch(sources, mapperFunction, envelopeConfigurationAction);

    public void WrapAndPublishBatch<TSource, TMessage, TArgument>(
        IEnumerable<TSource> sources,
        Func<TSource, TArgument, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource, TArgument> envelopeConfigurationAction,
        TArgument argument)
        where TMessage : class =>
        _publisher.WrapAndPublishBatch(sources, mapperFunction, envelopeConfigurationAction, argument);

    public void WrapAndPublishBatch<TMessage>(
        IAsyncEnumerable<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction = null)
        where TMessage : class =>
        _publisher.WrapAndPublishBatch(messages, envelopeConfigurationAction);

    public void WrapAndPublishBatch<TMessage, TArgument>(
        IAsyncEnumerable<TMessage?> messages,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument)
        where TMessage : class =>
        _publisher.WrapAndPublishBatch(messages, envelopeConfigurationAction, argument);

    public void WrapAndPublishBatch<TSource, TMessage>(
        IAsyncEnumerable<TSource> sources,
        Func<TSource, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource>? envelopeConfigurationAction = null)
        where TMessage : class =>
        _publisher.WrapAndPublishBatch(sources, mapperFunction, envelopeConfigurationAction);

    public void WrapAndPublishBatch<TSource, TMessage, TArgument>(
        IAsyncEnumerable<TSource> sources,
        Func<TSource, TArgument, TMessage?> mapperFunction,
        Action<IOutboundEnvelope<TMessage>, TSource, TArgument> envelopeConfigurationAction,
        TArgument argument)
        where TMessage : class =>
        _publisher.WrapAndPublishBatch(sources, mapperFunction, envelopeConfigurationAction, argument);

    public Task WrapAndPublishAsync<TMessage>(TMessage? message, CancellationToken cancellationToken = default)
        where TMessage : class =>
        _publisher.WrapAndPublishAsync(message, cancellationToken);

    public Task WrapAndPublishAsync<TMessage>(
        TMessage? message,
        Action<IOutboundEnvelope<TMessage>>? envelopeConfigurationAction,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        _publisher.WrapAndPublishAsync(message, envelopeConfigurationAction, cancellationToken);

    public Task WrapAndPublishAsync<TMessage, TArgument>(
        TMessage? message,
        Action<IOutboundEnvelope<TMessage>, TArgument> envelopeConfigurationAction,
        TArgument argument,
        CancellationToken cancellationToken = default)
        where TMessage : class =>
        _publisher.WrapAndPublishAsync(message, envelopeConfigurationAction, argument, cancellationToken);
}
