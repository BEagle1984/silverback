// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
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
    public IProduceStrategyImplementation Build(IServiceProvider serviceProvider, ProducerEndpointConfiguration endpointConfiguration)
    {
        _outboxWriter ??= serviceProvider.GetRequiredService<OutboxWriterFactory>().GetWriter(Settings);
        _logger ??= serviceProvider.GetRequiredService<IProducerLogger<OutboxProduceStrategy>>();

        return new OutboxProduceStrategyImplementation(_outboxWriter, endpointConfiguration, serviceProvider, _logger);
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

        private readonly IServiceProvider _serviceProvider;

        private readonly IProducerLogger<OutboxProduceStrategy> _logger;

        private readonly SilverbackContext _context;

        private DelegatedProducer? _producer;

        public OutboxProduceStrategyImplementation(
            IOutboxWriter outboxWriter,
            ProducerEndpointConfiguration configuration,
            IServiceProvider serviceProvider,
            IProducerLogger<OutboxProduceStrategy> logger)
        {
            _outboxWriter = outboxWriter;
            _configuration = configuration;
            _serviceProvider = serviceProvider;
            _logger = logger;

            _context = serviceProvider.GetRequiredService<SilverbackContext>();
        }

        public async Task ProduceAsync(IOutboundEnvelope envelope)
        {
            _producer ??= new DelegatedProducer(
                finalEnvelope => _outboxWriter.AddAsync(MapToOutboxMessage(finalEnvelope), _context),
                _configuration,
                _serviceProvider);

            await _producer.ProduceAsync(envelope).ConfigureAwait(false);
            _logger.LogStoringIntoOutbox(envelope);
        }

        [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Awaited")]
        public async Task ProduceAsync(IEnumerable<IOutboundEnvelope> envelopes)
        {
            using MessageStreamEnumerable<IOutboundEnvelope> stream = new();
            using DelegatedProducer producer = new(envelope => stream.PushAsync(envelope), _configuration, _serviceProvider);

            Task.Run(
                async () =>
                {
                    foreach (IOutboundEnvelope envelope in envelopes)
                    {
                        await producer.ProduceAsync(envelope).ConfigureAwait(false);
                    }

                    await stream.CompleteAsync().ConfigureAwait(false);
                }).FireAndForget();

            await _outboxWriter.AddAsync(stream.AsEnumerable().Select(MapToOutboxMessage), _context).ConfigureAwait(false);
        }

        [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Awaited")]
        public async Task ProduceAsync(IAsyncEnumerable<IOutboundEnvelope> envelopes)
        {
            using MessageStreamEnumerable<IOutboundEnvelope> stream = new();
            using DelegatedProducer producer = new(envelope => stream.PushAsync(envelope), _configuration, _serviceProvider);

            Task.Run(
                async () =>
                {
                    await foreach (IOutboundEnvelope envelope in envelopes)
                    {
                        await producer.ProduceAsync(envelope).ConfigureAwait(false);
                    }

                    await stream.CompleteAsync().ConfigureAwait(false);
                }).FireAndForget();

            await _outboxWriter.AddAsync(stream.AsEnumerable().Select(MapToOutboxMessage), _context).ConfigureAwait(false);
        }

        public void Dispose() => _producer?.Dispose();

        private OutboxMessage MapToOutboxMessage(IOutboundEnvelope envelope) =>
            new(
                envelope.RawMessage.ReadAll(),
                envelope.Headers,
                new OutboxMessageEndpoint(
                    _configuration.FriendlyName ?? string.Empty,
                    GetSerializedEndpoint(envelope.Endpoint)));

        private string? GetSerializedEndpoint(ProducerEndpoint endpoint) =>
            _configuration.Endpoint is IDynamicProducerEndpointResolver dynamicEndpointProvider
                ? dynamicEndpointProvider.Serialize(endpoint)
                : null;
    }
}
