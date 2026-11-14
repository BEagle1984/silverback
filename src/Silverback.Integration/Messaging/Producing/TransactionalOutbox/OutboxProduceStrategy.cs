// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Producing.EndpointResolvers;
using Silverback.Util;

namespace Silverback.Messaging.Producing.TransactionalOutbox;

/// <summary>
///     The messages are stored in the transactional outbox table. The operation can therefore be included in the database transaction
///     applying the message side effects to the local database. The <see cref="IOutboxWorker" /> takes care of asynchronously sending
///     the messages to the message broker.
/// </summary>
public sealed class OutboxProduceStrategy : IProduceStrategy, IEquatable<OutboxProduceStrategy>
{
    private IOutboxWriter? _outboxWriter;

    private ISilverbackLogger<OutboxProduceStrategy>? _logger;

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
        _logger ??= context.ServiceProvider.GetRequiredService<ISilverbackLogger<OutboxProduceStrategy>>();

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

        private readonly ISilverbackLogger<OutboxProduceStrategy> _logger;

        private readonly ISilverbackContext _context;

        private DelegatedProducer<DelegatedProducerState>? _producer;

        public OutboxProduceStrategyImplementation(
            IOutboxWriter outboxWriter,
            ProducerEndpointConfiguration configuration,
            ISilverbackContext context,
            ISilverbackLogger<OutboxProduceStrategy> logger)
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

        public Task ProduceAsync(IEnumerable<IOutboundEnvelope> envelopes, CancellationToken cancellationToken) =>
            ProduceCoreAsync(
                async (producer, produceCancellationToken) =>
                {
                    foreach (IOutboundEnvelope envelope in envelopes)
                    {
                        produceCancellationToken.ThrowIfCancellationRequested();

                        await producer.ProduceAsync(envelope, produceCancellationToken).ConfigureAwait(false);
                    }
                },
                cancellationToken);

        public Task ProduceAsync(IAsyncEnumerable<IOutboundEnvelope> envelopes, CancellationToken cancellationToken) =>
            ProduceCoreAsync(
                async (producer, produceCancellationToken) =>
                {
                    await foreach (IOutboundEnvelope envelope in envelopes.WithCancellation(produceCancellationToken))
                    {
                        produceCancellationToken.ThrowIfCancellationRequested();

                        await producer.ProduceAsync(envelope, produceCancellationToken).ConfigureAwait(false);
                    }
                },
                cancellationToken);

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

        [SuppressMessage("ReSharper", "AccessToDisposedClosure", Justification = "Awaited")]
        [SuppressMessage("ReSharper", "AccessToModifiedClosure", Justification = "Shared with the producer task on purpose, through Interlocked")]
        [SuppressMessage("Reliability", "CA2025:Do not pass \'IDisposable\' instances into unawaited tasks", Justification = "The task is awaited before disposing")]
        private async Task ProduceCoreAsync(
            Func<IProducer, CancellationToken, Task> produceEnvelopesAsync,
            CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            using MessageStreamEnumerable<IOutboundEnvelope> stream = new();
            using DelegatedProducer<MessageStreamEnumerable<IOutboundEnvelope>> producer = new(
                static (finalEnvelope, stream, finalCancellationToken) => stream.PushAsync(finalEnvelope, cancellationToken: finalCancellationToken),
                _configuration,
                stream,
                _context.ServiceProvider);
            using CancellationTokenSource produceTokenSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            // Counts the failed sides (producer and writer): the first failure is the root cause, since it aborts the stream and makes
            // the other side fail as well (on cancellation, either side can fail first, both with an OperationCanceledException)
            int failuresCount = 0;

            // Don't pass the cancellation token to Task.Run: the task must always run, to complete or abort the stream
            Task produceTask = Task.Run(
                async () =>
                {
                    try
                    {
                        await produceEnvelopesAsync(producer, produceTokenSource.Token).ConfigureAwait(false);

                        // Complete the stream regardless of the cancellation: no push is pending, so it cannot block
                        await stream.CompleteAsync(CancellationToken.None).ConfigureAwait(false);
                    }
                    catch (Exception)
                    {
                        // Abort the stream to make the outbox writer fail, instead of waiting forever for the next message
                        Interlocked.Increment(ref failuresCount);
                        stream.Abort();
                        throw;
                    }
                },
                CancellationToken.None);

            try
            {
                await _outboxWriter.AddAsync(stream.AsEnumerable().Select(MapToOutboxMessage), _context, cancellationToken).ConfigureAwait(false);
            }
            catch (Exception)
            {
                bool isProducerFailureRootCause = Interlocked.Increment(ref failuresCount) > 1;

                // Release the pending push, if any, and stop producing
                stream.Abort();

                try
                {
                    await produceTokenSource.CancelAsync().ConfigureAwait(false);
                }
                catch (AggregateException)
                {
                    // Ignore the exceptions thrown by the cancellation callbacks: they aren't the root cause
                }

                // Never dispose the stream and the producer while the task is still using them
                await produceTask.ConfigureAwait(ConfigureAwaitOptions.SuppressThrowing);

                // Rethrow the producer exception, if the writer failed because of it
                if (isProducerFailureRootCause)
                    await produceTask.ConfigureAwait(false);

                throw;
            }

            await produceTask.ConfigureAwait(false);
        }

        private record struct DelegatedProducerState(IOutboxWriter OutboxWriter, ISilverbackContext Context);
    }
}
