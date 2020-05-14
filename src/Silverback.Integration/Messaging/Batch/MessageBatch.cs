// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Timer = System.Timers.Timer;

namespace Silverback.Messaging.Batch
{
    // TODO: Test? (or implicitly tested with InboundConnector?)
    internal sealed class MessageBatch : IDisposable
    {
        private readonly BatchSettings _settings;

        private readonly IErrorPolicy? _errorPolicy;

        private readonly IConsumer _consumer;

        private readonly IErrorPolicyHelper _errorPolicyHelper;

        private readonly IServiceProvider _serviceProvider;

        private readonly ILogger _logger;

        private readonly MessageLogger _messageLogger;

        private readonly List<IRawInboundEnvelope> _envelopes;

        private readonly Timer? _waitTimer;

        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        private ConsumerBehaviorHandler? _messagesHandler;

        private ConsumerBehaviorHandler? _commitHandler;

        private ConsumerBehaviorErrorHandler? _rollbackHandler;

        private Exception? _processingException;

        public MessageBatch(
            BatchSettings settings,
            IErrorPolicy? errorPolicy,
            IConsumer consumer,
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _errorPolicyHelper = serviceProvider.GetRequiredService<IErrorPolicyHelper>();

            _errorPolicy = errorPolicy;
            _consumer = consumer ?? throw new ArgumentNullException(nameof(consumer));
            _settings = settings;

            _envelopes = new List<IRawInboundEnvelope>(_settings.Size);

            if (_settings.MaxWaitTime < TimeSpan.MaxValue)
            {
                _waitTimer = new Timer(_settings.MaxWaitTime.TotalMilliseconds);
                _waitTimer.Elapsed += OnWaitTimerElapsed;
            }

            _logger = serviceProvider.GetRequiredService<ILogger<MessageBatch>>();
            _messageLogger = serviceProvider.GetRequiredService<MessageLogger>();
        }

        public Guid CurrentBatchId { get; private set; }

        public int CurrentSize => _envelopes.Count;

        public void BindOnce(
            ConsumerBehaviorHandler messagesHandler,
            ConsumerBehaviorHandler commitHandler,
            ConsumerBehaviorErrorHandler rollbackHandler)
        {
            if (_messagesHandler != null)
                return;

            _messagesHandler = messagesHandler ?? throw new ArgumentNullException(nameof(messagesHandler));
            _commitHandler = commitHandler ?? throw new ArgumentNullException(nameof(commitHandler));
            _rollbackHandler = rollbackHandler ?? throw new ArgumentNullException(nameof(rollbackHandler));
        }

        public async Task AddMessages(IReadOnlyCollection<IRawInboundEnvelope> envelopes)
        {
            // TODO: Check this!
            if (_processingException != null)
            {
                throw new BatchException(
                    "Cannot add to the batch because the processing of the previous " +
                    "batch failed. See inner exception for details.",
                    _processingException);
            }

            await _semaphore.WaitAsync();

            try
            {
                _envelopes.AddRange(envelopes);

                _envelopes.ForEach(
                    envelope =>
                        _messageLogger.LogInformation(_logger, "Message added to batch.", envelope));

                if (_envelopes.Count == 1)
                {
                    CurrentBatchId = Guid.NewGuid();
                    _waitTimer?.Start();

                    using var scope = _serviceProvider.CreateScope();
                    await scope.ServiceProvider.GetRequiredService<IPublisher>()
                        .PublishAsync(new BatchStartedEvent(CurrentBatchId));
                }
                else if (_envelopes.Count == _settings.Size)
                {
                    await ProcessBatch();
                }
            }
            finally
            {
                _semaphore.Release();
            }
        }

        public void Dispose()
        {
            _semaphore.Dispose();
        }

        private void OnWaitTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _waitTimer?.Stop();

            Task.Run(
                async () =>
                {
                    await _semaphore.WaitAsync();

                    try
                    {
                        if (_envelopes.Any())
                            await ProcessBatch();
                    }
                    finally
                    {
                        _semaphore.Release();
                    }
                });
        }

        private async Task ProcessBatch()
        {
            try
            {
                AddHeaders(_envelopes);

                await _errorPolicyHelper.TryProcessAsync(
                    new ConsumerPipelineContext(_envelopes, _consumer),
                    _errorPolicy,
                    ForwardMessages,
                    Commit,
                    Rollback);

                _envelopes.Clear();
            }
            catch (Exception ex)
            {
                _processingException = ex;
                throw new BatchException("Failed to process batch. See inner exception for details.", ex);
            }
        }

        private void AddHeaders(IReadOnlyCollection<IRawInboundEnvelope> envelopes)
        {
            foreach (var envelope in envelopes)
            {
                envelope.Headers.AddOrReplace(DefaultMessageHeaders.BatchId, CurrentBatchId);
                envelope.Headers.AddOrReplace(DefaultMessageHeaders.BatchSize, CurrentSize);
            }
        }

        private async Task ForwardMessages(ConsumerPipelineContext context, IServiceProvider serviceProvider)
        {
            await serviceProvider.GetRequiredService<IPublisher>().PublishAsync(
                new BatchCompleteEvent(CurrentBatchId, context.Envelopes));

            if (_messagesHandler == null)
                throw new InvalidOperationException("No message handler has been provided.");

            await _messagesHandler(context, serviceProvider);
        }

        private async Task Commit(ConsumerPipelineContext context, IServiceProvider serviceProvider)
        {
            await serviceProvider.GetRequiredService<IPublisher>().PublishAsync(
                new BatchProcessedEvent(CurrentBatchId, context.Envelopes));

            if (_commitHandler == null)
                throw new InvalidOperationException("No commit handler has been provided.");

            await _commitHandler(context, serviceProvider);
        }

        private async Task Rollback(
            ConsumerPipelineContext context,
            IServiceProvider serviceProvider,
            Exception exception)
        {
            await serviceProvider.GetRequiredService<IPublisher>().PublishAsync(
                new BatchAbortedEvent(CurrentBatchId, context.Envelopes, exception));

            if (_rollbackHandler == null)
                throw new InvalidOperationException("No rollback handler has been provided.");

            await _rollbackHandler(context, serviceProvider, exception);
        }
    }
}
