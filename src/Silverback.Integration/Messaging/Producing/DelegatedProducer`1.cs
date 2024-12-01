// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading;
using System.Threading.Tasks;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Producing;

internal class DelegatedProducer<T> : DelegatedProducer
{
    private readonly T _state;

    private readonly ProduceDelegate<T> _delegate;

    public DelegatedProducer(
        ProduceDelegate<T> produceDelegate,
        ProducerEndpointConfiguration endpointConfiguration,
        T state,
        IServiceProvider serviceProvider)
        : base(endpointConfiguration, serviceProvider)
    {
        _delegate = Check.NotNull(produceDelegate, nameof(produceDelegate));
        _state = state;
    }

    /// <inheritdoc cref="Producer.ProduceCore(IOutboundEnvelope)" />
    protected override IBrokerMessageIdentifier ProduceCore(IOutboundEnvelope envelope) =>
        throw new NotSupportedException("Only asynchronous operations are supported.");

    /// <inheritdoc cref="Producer.ProduceCore{TState}(IOutboundEnvelope,Action{IBrokerMessageIdentifier,TState},Action{Exception,TState},TState)" />
    protected override void ProduceCore<TState>(
        IOutboundEnvelope envelope,
        Action<IBrokerMessageIdentifier?, TState> onSuccess,
        Action<Exception, TState> onError,
        TState state) => throw new NotSupportedException("Only asynchronous operations are supported.");

    /// <inheritdoc cref="Producer.ProduceCoreAsync(IOutboundEnvelope,CancellationToken)" />
    protected override async ValueTask<IBrokerMessageIdentifier?> ProduceCoreAsync(
        IOutboundEnvelope envelope,
        CancellationToken cancellationToken)
    {
        await _delegate.Invoke(envelope, _state, cancellationToken).ConfigureAwait(false);

        return null;
    }
}
