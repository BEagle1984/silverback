// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
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
        private static readonly TimeSpan ConsumerStopWaitTimeout = TimeSpan.FromMinutes(1);

        private readonly IReadOnlyList<IConsumerBehavior> _behaviors;

        private readonly ISilverbackIntegrationLogger<Consumer> _logger;

        private readonly ConsumerStatusInfo _statusInfo = new ConsumerStatusInfo();

        private readonly ConcurrentDictionary<IBrokerMessageIdentifier, int> _failedAttemptsDictionary =
            new ConcurrentDictionary<IBrokerMessageIdentifier, int>();

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
            ServiceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
            _logger = Check.NotNull(logger, nameof(logger));

            Endpoint.Validate();
        }

        /// <inheritdoc cref="IConsumer.Id" />
        public Guid Id { get; } = Guid.NewGuid();

        /// <inheritdoc cref="IConsumer.Broker" />
        public IBroker Broker { get; }

        /// <inheritdoc cref="IConsumer.Endpoint" />
        public IConsumerEndpoint Endpoint { get; }

        /// <inheritdoc cref="IConsumer.StatusInfo" />
        public IConsumerStatusInfo StatusInfo => _statusInfo;

        /// <inheritdoc cref="IConsumer.IsConnected" />
        public bool IsConnected { get; private set; }

        /// <inheritdoc cref="IConsumer.IsConsuming" />
        public bool IsConsuming { get; protected set; }

        /// <summary>
        ///     Gets the <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </summary>
        protected IServiceProvider ServiceProvider { get; }

        /// <summary>
        ///     Gets the <see cref="ISequenceStore" /> instances used by this consumer. Some brokers will require
        ///     multiple stores (e.g. the <c>KafkaConsumer</c> will create a store per each assigned partition).
        /// </summary>
        protected IList<ISequenceStore> SequenceStores { get; } = new List<ISequenceStore>();

        /// <summary>
        ///     Gets a value indicating whether the consumer is being disconnected.
        /// </summary>
        protected bool IsDisconnecting { get; private set; }

        /// <inheritdoc cref="IConsumer.CommitAsync(IBrokerMessageIdentifier)" />
        public Task CommitAsync(IBrokerMessageIdentifier brokerMessageIdentifier)
        {
            Check.NotNull(brokerMessageIdentifier, nameof(brokerMessageIdentifier));

            return CommitAsync(new[] { brokerMessageIdentifier });
        }

        /// <inheritdoc cref="IConsumer.CommitAsync(IReadOnlyCollection{IBrokerMessageIdentifier})" />
        public async Task CommitAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers)
        {
            Check.NotNull(brokerMessageIdentifiers, nameof(brokerMessageIdentifiers));

            await CommitCoreAsync(brokerMessageIdentifiers).ConfigureAwait(false);

            if (_failedAttemptsDictionary.IsEmpty)
                return;

            // TODO: Is this the most efficient way to remove a bunch of items?
            foreach (var messageIdentifier in brokerMessageIdentifiers)
            {
                _failedAttemptsDictionary.TryRemove(messageIdentifier, out _);
            }
        }

        /// <inheritdoc cref="IConsumer.RollbackAsync(IBrokerMessageIdentifier)" />
        public Task RollbackAsync(IBrokerMessageIdentifier brokerMessageIdentifier)
        {
            Check.NotNull(brokerMessageIdentifier, nameof(brokerMessageIdentifier));

            return RollbackAsync(new[] { brokerMessageIdentifier });
        }

        /// <inheritdoc cref="IConsumer.RollbackAsync(IReadOnlyCollection{IBrokerMessageIdentifier})" />
        public Task RollbackAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers)
        {
            Check.NotNull(brokerMessageIdentifiers, nameof(brokerMessageIdentifiers));

            return RollbackCoreAsync(brokerMessageIdentifiers);
        }

        /// <inheritdoc cref="IConsumer.ConnectAsync" />
        public async Task ConnectAsync()
        {
            if (IsConnected)
                return;

            await ConnectCoreAsync().ConfigureAwait(false);

            IsConnected = true;
            _statusInfo.SetConnected();
            _logger.LogDebug(
                IntegrationEventIds.ConsumerConnected,
                "Connected consumer to endpoint {endpoint}.",
                Endpoint.Name);

            Start();
        }

        /// <inheritdoc cref="IConsumer.DisconnectAsync" />
        public async Task DisconnectAsync()
        {
            if (!IsConnected)
                return;

            IsDisconnecting = true;

            // Ensure that StopCore is called in any case to avoid deadlocks (when the consumer loop is initialized
            // but not started)
            if (IsConsuming)
                Stop();
            else
                StopCore();

            if (SequenceStores.Count > 0)
            {
                await SequenceStores
                    .SelectMany(store => store)
                    .ToList()
                    .ForEachAsync(sequence => sequence.AbortAsync(SequenceAbortReason.ConsumerAborted))
                    .ConfigureAwait(false);
            }

            using (var cancellationTokenSource = new CancellationTokenSource(ConsumerStopWaitTimeout))
            {
                _logger.LogTrace(IntegrationEventIds.LowLevelTracing, "Waiting until consumer stops...");

                try
                {
                    await WaitUntilConsumingStoppedAsync(cancellationTokenSource.Token).ConfigureAwait(false);
                    _logger.LogTrace(IntegrationEventIds.LowLevelTracing, "Consumer stopped.");
                }
                catch (OperationCanceledException)
                {
                    _logger.LogError(
                        IntegrationEventIds.LowLevelTracing,
                        "The timeout elapsed before the consumer stopped.");
                }
            }

            await DisconnectCoreAsync().ConfigureAwait(false);

            SequenceStores.ForEach(store => store.Dispose());
            SequenceStores.Clear();

            IsConnected = false;
            _statusInfo.SetDisconnected();
            _logger.LogDebug(
                IntegrationEventIds.ConsumerDisconnected,
                "Disconnected consumer from endpoint {endpoint}.",
                Endpoint.Name);

            IsDisconnecting = false;
        }

        /// <inheritdoc cref="IConsumer.Start" />
        public void Start()
        {
            if (IsConsuming)
                return;

            StartCore();

            IsConsuming = true;
        }

        /// <inheritdoc cref="IConsumer.Stop" />
        public void Stop()
        {
            if (!IsConsuming)
                return;

            StopCore();

            IsConsuming = false;
        }

        /// <inheritdoc cref="IConsumer.IncrementFailedAttempts" />
        public int IncrementFailedAttempts(IRawInboundEnvelope envelope)
        {
            Check.NotNull(envelope, nameof(envelope));

            return _failedAttemptsDictionary.AddOrUpdate(
                envelope.BrokerMessageIdentifier,
                _ => envelope.Headers.GetValueOrDefault<int>(DefaultMessageHeaders.FailedAttempts) + 1,
                (_, count) => count + 1);
        }

        /// <inheritdoc cref="IDisposable.Dispose" />
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        ///     Gets the <see cref="ISequenceStore" /> instances used by this consumer. Some brokers will require
        ///     multiple stores (e.g. the <c>KafkaConsumer</c> will create a store per each assigned partition).
        /// </summary>
        /// <returns>
        ///     The list of <see cref="ISequenceStore" />.
        /// </returns>
        public IReadOnlyList<ISequenceStore> GetCurrentSequenceStores() => SequenceStores.AsReadOnlyList();

        /// <summary>
        ///     Connects to the message broker.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected abstract Task ConnectCoreAsync();

        /// <summary>
        ///     Disconnects from the message broker.
        /// </summary>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected abstract Task DisconnectCoreAsync();

        /// <summary>
        ///     Starts consuming. Called to resume consuming after <see cref="Stop" /> has been called.
        /// </summary>
        protected abstract void StartCore();

        /// <summary>
        ///     Stops consuming while staying connected to the message broker.
        /// </summary>
        protected abstract void StopCore();

        /// <summary>
        ///     Commits the specified messages sending the acknowledgement to the message broker.
        /// </summary>
        /// <param name="brokerMessageIdentifiers">
        ///     The identifiers of to message be committed.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected abstract Task CommitCoreAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers);

        /// <summary>
        ///     If necessary notifies the message broker that the specified messages couldn't be processed
        ///     successfully, to ensure that they will be consumed again.
        /// </summary>
        /// <param name="brokerMessageIdentifiers">
        ///     The identifiers of to message be rolled back.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected abstract Task RollbackCoreAsync(IReadOnlyCollection<IBrokerMessageIdentifier> brokerMessageIdentifiers);

        /// <summary>
        ///     Waits until the consuming is stopped.
        /// </summary>
        /// <param name="cancellationToken">
        ///     A <see cref="CancellationToken" /> to observe while waiting for the task to complete.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation.
        /// </returns>
        protected abstract Task WaitUntilConsumingStoppedAsync(CancellationToken cancellationToken);

        /// <summary>
        ///     Returns the <see cref="ISequenceStore" /> to be used to store the pending sequences.
        /// </summary>
        /// <param name="brokerMessageIdentifier">
        ///     The message identifier (the offset in Kafka) may determine which store is being used. For example a
        ///     dedicated sequence store is used per each Kafka partition, since they may be processed concurrently.
        /// </param>
        /// <returns>
        ///     The <see cref="ISequenceStore" />.
        /// </returns>
        protected virtual ISequenceStore GetSequenceStore(IBrokerMessageIdentifier brokerMessageIdentifier)
        {
            if (SequenceStores.Count == 0)
            {
                lock (SequenceStores)
                {
                    SequenceStores.Add(ServiceProvider.GetRequiredService<ISequenceStore>());
                }
            }

            return SequenceStores.FirstOrDefault() ??
                   throw new InvalidOperationException("The sequence store is not initialized.");
        }

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
        /// <param name="brokerMessageIdentifier">
        ///     The identifier of the consumed message.
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
            IBrokerMessageIdentifier brokerMessageIdentifier,
            IDictionary<string, string>? additionalLogData)
        {
            var envelope = new RawInboundEnvelope(
                message,
                headers,
                Endpoint,
                sourceEndpointName,
                brokerMessageIdentifier,
                additionalLogData);

            _statusInfo.RecordConsumedMessage(brokerMessageIdentifier);

            var consumerPipelineContext = new ConsumerPipelineContext(
                envelope,
                this,
                GetSequenceStore(brokerMessageIdentifier),
                ServiceProvider);

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
                AsyncHelper.RunSynchronously(DisconnectAsync);
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
