// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Sequences;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    /// <inheritdoc cref="IConsumer" />
    public abstract class Consumer : IConsumer, IDisposable
    {
        // ReSharper disable once StaticMemberInGenericType
        private static readonly TimeSpan SequenceAbortTaskAwaitTimeout = TimeSpan.FromSeconds(60);

        private readonly IReadOnlyList<IConsumerBehavior> _behaviors;

        private readonly IServiceProvider _serviceProvider;

        private readonly ISilverbackIntegrationLogger<Consumer> _logger;

        private readonly ConsumerStatusInfo _statusInfo = new ConsumerStatusInfo();

        private readonly ConcurrentDictionary<IOffset, int> _failedAttemptsDictionary =
            new ConcurrentDictionary<IOffset, int>();

        private ISequenceStore? _sequenceStore; // TODO: Should be per partition

        /// <summary>
        ///     Initializes a new instance of the <see cref="Consumer" /> class.
        /// </summary>
        /// <param name="broker">
        ///     The <see cref="IBroker" /> that is instantiating the consumer.
        /// </param>
        /// <param name="endpoint">
        ///     The endpoint to be consumed.
        /// </param>
        /// <param name="behaviorsProvider">
        ///     The <see cref="IBrokerBehaviorsProvider{TBehavior}" />.
        /// </param>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ISilverbackIntegrationLogger" />.
        /// </param>
        protected Consumer(
            IBroker broker,
            IConsumerEndpoint endpoint,
            IBrokerBehaviorsProvider<IConsumerBehavior> behaviorsProvider,
            IServiceProvider serviceProvider,
            ISilverbackIntegrationLogger<Consumer> logger)
        {
            Broker = Check.NotNull(broker, nameof(broker));
            Endpoint = Check.NotNull(endpoint, nameof(endpoint));

            _behaviors = Check.NotNull(behaviorsProvider, nameof(behaviorsProvider)).GetBehaviorsList();
            _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
            _logger = Check.NotNull(logger, nameof(logger));

            Endpoint.Validate();
        }

        /// <inheritdoc cref="IConsumer.Broker" />
        public IBroker Broker { get; }

        /// <inheritdoc cref="IConsumer.Endpoint" />
        public IConsumerEndpoint Endpoint { get; }

        /// <inheritdoc cref="IConsumer.StatusInfo" />
        public IConsumerStatusInfo StatusInfo => _statusInfo;

        /// <inheritdoc cref="IConsumer.IsConnected" />
        public bool IsConnected { get; private set; }

        /// <inheritdoc cref="IConsumer.CommitAsync(IOffset)" />
        public Task CommitAsync(IOffset offset) => CommitAsync(new[] { offset });

        /// <inheritdoc cref="IConsumer.CommitAsync(IReadOnlyCollection{IOffset})" />
        public abstract Task CommitAsync(IReadOnlyCollection<IOffset> offsets);

        /// <inheritdoc cref="IConsumer.RollbackAsync(IOffset)" />
        public Task RollbackAsync(IOffset offset) => RollbackAsync(new[] { offset });

        /// <inheritdoc cref="IConsumer.RollbackAsync(IReadOnlyCollection{IOffset})" />
        public abstract Task RollbackAsync(IReadOnlyCollection<IOffset> offsets);

        /// <inheritdoc cref="IConsumer.Connect" />
        public void Connect()
        {
            if (IsConnected)
                return;

            _sequenceStore = _serviceProvider.GetRequiredService<ISequenceStore>();

            ConnectCore();

            if (IsConnected)
                throw new InvalidOperationException("Already connected.");

            IsConnected = true;

            _statusInfo.SetConnected();

            _logger.LogDebug(
                IntegrationEventIds.ConsumerConnected,
                "Connected consumer to endpoint {endpoint}.",
                Endpoint.Name);
        }

        /// <inheritdoc cref="IConsumer.Disconnect" />
        public void Disconnect()
        {
            if (!IsConnected)
                return;

            _logger.LogTrace("Stopping consumer loop...");
            StopConsuming();
            _logger.LogTrace("Consumer loop stopped.");


            if (_sequenceStore != null)
            {
                _logger.LogTrace("Aborting pending sequences...");

                // ReSharper disable once AccessToDisposedClosure
                AsyncHelper.RunSynchronously(
                    () => _sequenceStore.ToList()
                        .ForEachAsync(sequence => sequence.AbortAsync(SequenceAbortReason.ConsumerAborted)));

                _logger.LogTrace("Pending sequences aborted.");
            }

            _logger.LogTrace("Disconnecting...");
            DisconnectCore();
            _logger.LogTrace("Disconnected.");

            _logger.LogTrace("Disposing sequence store...");
            _sequenceStore?.Dispose();
            _sequenceStore = null;
            _logger.LogTrace("Sequence store disposed.");

            IsConnected = false;
            _statusInfo.SetDisconnected();

            _logger.LogDebug(
                IntegrationEventIds.ConsumerDisconnected,
                "Disconnected consumer from endpoint {endpoint}.",
                Endpoint.Name);
        }

        /// <inheritdoc cref="IConsumer.Disconnect" />
        public virtual ISequenceStore GetSequenceStore(IOffset offset) =>
            _sequenceStore ?? throw new InvalidOperationException("The sequence store is not initialized.");

        /// <inheritdoc cref="IConsumer.IncrementFailedAttempts" />
        public int IncrementFailedAttempts(IRawInboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            return _failedAttemptsDictionary.AddOrUpdate(
                envelope.Offset,
                _ => envelope.Headers.GetValueOrDefault<int>(DefaultMessageHeaders.FailedAttempts) + 1,
                (_, count) => count + 1);
        }

        /// <inheritdoc cref="IConsumer.ClearFailedAttempts" />
        // TODO: Call it!
        public void ClearFailedAttempts(IRawInboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            _failedAttemptsDictionary.Remove(envelope.Offset, out _);
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Connects and starts consuming.
        /// </summary>
        protected abstract void ConnectCore();

        /// <summary>
        ///     Stops the consuming loop.
        /// </summary>
        protected abstract void StopConsuming();

        /// <summary>
        ///     Disconnects the consumer from the message broker.
        /// </summary>
        protected abstract void DisconnectCore();

        /// <summary>
        ///     Handles the consumed message invoking each <see cref="IConsumerBehavior" /> in the pipeline.
        /// </summary>
        /// <param name="message">
        ///     The body of the consumed message.
        /// </param>
        /// <param name="headers">
        ///     The headers of the consumed message.
        /// </param>
        /// <param name="sourceEndpointName">
        ///     The name of the actual endpoint (topic) where the message has been delivered.
        /// </param>
        /// <param name="offset">
        ///     The offset of the consumed message.
        /// </param>
        /// <param name="additionalLogData">
        ///     An optional dictionary containing the broker specific data to be logged when processing the consumed
        ///     message.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        [SuppressMessage("", "CA2000", Justification = "Context is disposed by the TransactionHandler")]
        protected virtual async Task HandleMessageAsync(
            byte[]? message,
            IReadOnlyCollection<MessageHeader> headers,
            string sourceEndpointName,
            IOffset offset,
            IDictionary<string, string>? additionalLogData)
        {
            var envelope = new RawInboundEnvelope(
                message,
                headers,
                Endpoint,
                sourceEndpointName,
                offset,
                additionalLogData);

            _statusInfo.RecordConsumedMessage(offset);

            var consumerPipelineContext = new ConsumerPipelineContext(
                envelope,
                this,
                GetSequenceStore(offset),
                _serviceProvider);

            await ExecutePipelineAsync(consumerPipelineContext).ConfigureAwait(false);
        }

        /// <summary>
        ///     Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged
        ///     resources.
        /// </summary>
        /// <param name="disposing">
        ///     A value indicating whether the method has been called by the <c>Dispose</c> method and not from the
        ///     finalizer.
        /// </param>
        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        protected virtual void Dispose(bool disposing)
        {
            if (!disposing)
                return;

            try
            {
                Disconnect();
            }
            catch (Exception ex)
            {
                _logger.LogWarning(
                    IntegrationEventIds.ConsumerDisposingError,
                    ex,
                    "Error occurred while disposing consumer from endpoint {endpoint}.",
                    Endpoint.Name);
            }
        }

        private async Task ExecutePipelineAsync(ConsumerPipelineContext context, int stepIndex = 0)
        {
            if (_behaviors.Count == 0 || stepIndex >= _behaviors.Count)
                return;

            await _behaviors[stepIndex].HandleAsync(
                    context,
                    nextContext => ExecutePipelineAsync(nextContext, stepIndex + 1))
                .ConfigureAwait(false);
        }
    }
}
