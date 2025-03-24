// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Util;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     The messages are stored in a the transactional outbox table. The operation is therefore included in the database transaction
///     applying the message side effects to the local database. The <see cref="IOutboxWorker" /> takes care of asynchronously sending
///     the messages to the message broker.
/// </summary>
public sealed class OutboxProduceStrategy : IProduceStrategy, IEquatable<OutboxProduceStrategy>
{
    private IOutboxWriter? _outboxWriter;

    private IProducerLogger<OutboxProduceStrategy>? _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxProduceStrategy" /> class.
    /// </summary>
    /// <param name="settings">
    ///     The outbox settings.
    /// </param>
    public OutboxProduceStrategy(OutboxSettings settings)
    {
        Settings = Check.NotNull(settings, nameof(settings));
    }

    /// <summary>
    ///     Gets the outbox settings.
    /// </summary>
    public OutboxSettings Settings { get; }

    /// <inheritdoc cref="op_Equality" />
    public static bool operator ==(OutboxProduceStrategy? left, OutboxProduceStrategy? right) => Equals(left, right);

    /// <inheritdoc cref="op_Inequality" />
    public static bool operator !=(OutboxProduceStrategy? left, OutboxProduceStrategy? right) => !Equals(left, right);

    /// <inheritdoc cref="IProduceStrategy.Build" />
    public IProduceStrategyImplementation Build(ISilverbackContext context, ProducerEndpointConfiguration endpointConfiguration)
    {
        Check.NotNull(context, nameof(context));

        _outboxWriter ??= context.ServiceProvider.GetRequiredService<OutboxWriterFactory>().GetWriter(Settings, context.ServiceProvider);
        _logger ??= context.ServiceProvider.GetRequiredService<IProducerLogger<OutboxProduceStrategy>>();

        return new OutboxProduceStrategyImplementation(_outboxWriter, endpointConfiguration, context, _logger);
    }

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(OutboxProduceStrategy? other) => other?.Settings == Settings;

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(IProduceStrategy? other) => other is OutboxProduceStrategy otherOutboxStrategy && Equals(otherOutboxStrategy);

    /// <inheritdoc cref="object.Equals(object)" />
    public override bool Equals(object? obj) => obj is OutboxProduceStrategy otherOutboxStrategy && Equals(otherOutboxStrategy);

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() => Settings.GetHashCode();

    private sealed class OutboxProduceStrategyImplementation : IProduceStrategyImplementation, IDisposable
    {
        private readonly IOutboxWriter _outboxWriter;

        private readonly ProducerEndpointConfiguration _configuration;

        private readonly IProducerLogger<OutboxProduceStrategy> _logger;

        private readonly ISilverbackContext _context;

        private DelegatedProducer<DelegatedProducerState>? _producer;

        public OutboxProduceStrategyImplementation(
            IOutboxWriter outboxWriter,
            ProducerEndpointConfiguration configuration,
            ISilverbackContext context,
            IProducerLogger<OutboxProduceStrategy> logger)
        {
            _outboxWriter = outboxWriter;
            _configuration = configuration;
            _context = context;
            _logger = logger;
        }

        public async Task ProduceAsync(IOutboundEnvelope envelope, CancellationToken cancellationToken)
        {
            _producer ??= new DelegatedProducer<DelegatedProducerState>(
                static (finalEnvelope, state, finalCancellationToken) => state.OutboxWriter.AddAsync(
                    MapToOutboxMessage(finalEnvelope),
                    state.Context,
                    finalCancellationToken),
                _configuration,
                new DelegatedProducerState(_outboxWriter, _context),
                _context.ServiceProvider);

            await _producer.ProduceAsync(envelope, cancellationToken).ConfigureAwait(false);
            _logger.LogStoringIntoOutbox(envelope);
        }

        [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Awaited")]
        public async Task ProduceAsync(IEnumerable<IOutboundEnvelope> envelopes, CancellationToken cancellationToken)
        {
            using MessageStreamEnumerable<IOutboundEnvelope> stream = new();
            using DelegatedProducer<MessageStreamEnumerable<IOutboundEnvelope>> producer = new(
                static (finalEnvelope, stream, finalCancellationToken) => stream.PushAsync(finalEnvelope, cancellationToken: finalCancellationToken),
                _configuration,
                stream,
                _context.ServiceProvider);

            Task.Run(
                async () =>
                {
                    foreach (IOutboundEnvelope envelope in envelopes)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        await producer.ProduceAsync(envelope, cancellationToken).ConfigureAwait(false);
                    }

                    await stream.CompleteAsync(cancellationToken).ConfigureAwait(false);
                },
                cancellationToken).FireAndForget();

            await _outboxWriter.AddAsync(stream.AsEnumerable().Select(MapToOutboxMessage), _context, cancellationToken).ConfigureAwait(false);
        }

        [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Awaited")]
        public async Task ProduceAsync(IAsyncEnumerable<IOutboundEnvelope> envelopes, CancellationToken cancellationToken)
        {
            using MessageStreamEnumerable<IOutboundEnvelope> stream = new();
            using DelegatedProducer<MessageStreamEnumerable<IOutboundEnvelope>> producer = new(
                static (finalEnvelope, stream, finalCancellationToken) => stream.PushAsync(finalEnvelope, cancellationToken: finalCancellationToken),
                _configuration,
                stream,
                _context.ServiceProvider);

            Task.Run(
                async () =>
                {
                    await foreach (IOutboundEnvelope envelope in envelopes.WithCancellation(cancellationToken))
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        await producer.ProduceAsync(envelope, cancellationToken).ConfigureAwait(false);
                    }

                    await stream.CompleteAsync(cancellationToken).ConfigureAwait(false);
                },
                cancellationToken).FireAndForget();

            await _outboxWriter.AddAsync(stream.AsEnumerable().Select(MapToOutboxMessage), _context, cancellationToken).ConfigureAwait(false);
        }

        public void Dispose() => _producer?.Dispose();

        private static OutboxMessage MapToOutboxMessage(IOutboundEnvelope envelope) =>
            new(
                envelope.RawMessage.ReadAll(),
                GetHeaders(envelope),
                envelope.EndpointConfiguration.FriendlyName ?? throw new InvalidOperationException("FriendlyName not set."));

        private static IEnumerable<MessageHeader> GetHeaders(IOutboundEnvelope envelope)
        {
            if (envelope.EndpointConfiguration.EndpointResolver is IDynamicProducerEndpointResolver dynamicEndpointProvider)
            {
                return envelope.Headers
                    .Append(new MessageHeader(DefaultMessageHeaders.SerializedEndpoint, dynamicEndpointProvider.GetSerializedEndpoint(envelope)));
            }

            return envelope.Headers;
        }

        private record struct DelegatedProducerState(IOutboxWriter OutboxWriter, ISilverbackContext Context);
    }
}
