// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Outbound.EndpointResolvers;

namespace Silverback.Messaging.Outbound.TransactionalOutbox;

/// <summary>
///     The messages are stored in a the transactional outbox table. The operation is therefore included in the database transaction
///     applying the message side effects to the local database. The <see cref="IOutboxWorker" /> takes care of asynchronously sending
///     the messages to the message broker.
/// </summary>
public sealed class OutboxProduceStrategy : IProduceStrategy, IEquatable<OutboxProduceStrategy>
{
    private IOutboxWriter? _outboxWriter;

    private IOutboundLogger<OutboxProduceStrategy>? _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="OutboxProduceStrategy" /> class.
    /// </summary>
    /// <param name="settings">
    ///     The outbox settings.
    /// </param>
    public OutboxProduceStrategy(OutboxSettings settings)
    {
        Settings = settings;
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
    public IProduceStrategyImplementation Build(IServiceProvider serviceProvider, ProducerConfiguration configuration)
    {
        _outboxWriter ??= serviceProvider.GetRequiredService<OutboxWriterFactory>().GetWriter(Settings);
        _logger ??= serviceProvider.GetRequiredService<IOutboundLogger<OutboxProduceStrategy>>();

        return new OutboxProduceStrategyImplementation(_outboxWriter, configuration, serviceProvider, _logger);
    }

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(OutboxProduceStrategy? other) => other?.Settings == Settings;

    /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
    public bool Equals(IProduceStrategy? other) => other is OutboxProduceStrategy otherOutboxStrategy && Equals(otherOutboxStrategy);

    /// <inheritdoc cref="object.Equals(object)" />
    public override bool Equals(object? obj) => obj is OutboxProduceStrategy otherOutboxStrategy && Equals(otherOutboxStrategy);

    /// <inheritdoc cref="object.GetHashCode" />
    public override int GetHashCode() => Settings.GetHashCode();

    private sealed class OutboxProduceStrategyImplementation : IProduceStrategyImplementation
    {
        private readonly IOutboxWriter _outboxWriter;

        private readonly ProducerConfiguration _configuration;

        private readonly IOutboundLogger<OutboxProduceStrategy> _logger;

        private readonly DelegatedProducer _producer;

        private readonly SilverbackContext _context;

        public OutboxProduceStrategyImplementation(
            IOutboxWriter outboxWriter,
            ProducerConfiguration configuration,
            IServiceProvider serviceProvider,
            IOutboundLogger<OutboxProduceStrategy> logger)
        {
            _outboxWriter = outboxWriter;
            _configuration = configuration;
            _logger = logger;

            _producer = new DelegatedProducer(WriteToOutboxAsync, _configuration, serviceProvider);
            _context = serviceProvider.GetRequiredService<SilverbackContext>();
        }

        public async Task ProduceAsync(IOutboundEnvelope envelope)
        {
            await _producer.ProduceAsync(envelope).ConfigureAwait(false);
            _logger.LogWrittenToOutbox(envelope);
        }

        private async Task WriteToOutboxAsync(
            object? message,
            byte[]? messageBytes,
            IReadOnlyCollection<MessageHeader>? headers,
            ProducerEndpoint endpoint)
        {
            await _outboxWriter.AddAsync(
                    new OutboxMessage(
                        message?.GetType(),
                        messageBytes,
                        headers,
                        new OutboxMessageEndpoint(
                            _configuration.RawName,
                            _configuration.FriendlyName,
                            await GetSerializedEndpointAsync(endpoint).ConfigureAwait(false))),
                    _context)
                .ConfigureAwait(false);
        }

        private async ValueTask<byte[]?> GetSerializedEndpointAsync(ProducerEndpoint endpoint) =>
            _configuration.Endpoint is IDynamicProducerEndpointResolver dynamicEndpointProvider
                ? await dynamicEndpointProvider.SerializeAsync(endpoint).ConfigureAwait(false)
                : null;
    }
}
