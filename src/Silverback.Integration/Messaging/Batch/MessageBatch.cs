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
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Timer = System.Timers.Timer;

namespace Silverback.Messaging.Batch
{
    // TODO: Test? (or implicitly tested with InboundConnector?)
    public class MessageBatch
    {
        private readonly BatchSettings _settings;
        private readonly IErrorPolicy _errorPolicy;
        private readonly ErrorPolicyHelper _errorPolicyHelper;

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly MessageLogger _messageLogger;

        private readonly List<IRawInboundEnvelope> _envelopes;
        private readonly Timer _waitTimer;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        private Func<IReadOnlyCollection<IRawInboundEnvelope>, IServiceProvider, Task> _messagesHandler;
        private Func<IReadOnlyCollection<IRawInboundEnvelope>, IServiceProvider, Task> _commitHandler;
        private Func<IReadOnlyCollection<IRawInboundEnvelope>, IServiceProvider, Task> _rollbackHandler;
        private Exception _processingException;

        public MessageBatch(
            BatchSettings settings,
            IErrorPolicy errorPolicy,
            IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _errorPolicyHelper = serviceProvider.GetRequiredService<ErrorPolicyHelper>();

            _errorPolicy = errorPolicy;
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
            Func<IReadOnlyCollection<IRawInboundEnvelope>, IServiceProvider, Task> messagesHandler,
            Func<IReadOnlyCollection<IRawInboundEnvelope>, IServiceProvider, Task> commitHandler,
            Func<IReadOnlyCollection<IRawInboundEnvelope>, IServiceProvider, Task> rollbackHandler
        )
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
                throw new SilverbackException(
                    "Cannot add to the batch because the processing of the previous batch failed. See inner exception for details.",
                    _processingException);

            await _semaphore.WaitAsync();

            try
            {
                _envelopes.AddRange(envelopes);

                _envelopes.ForEach(envelope =>
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

        private void OnWaitTimerElapsed(object sender, ElapsedEventArgs e)
        {
            _waitTimer?.Stop();

            Task.Run(async () =>
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
                }
            );
        }

        private async Task ProcessBatch()
        {
            try
            {
                AddHeaders(_envelopes);

                await _errorPolicyHelper.TryProcessAsync(
                    _envelopes,
                    _errorPolicy,
                    ProcessEachMessageAndPublishEvents);

                _envelopes.Clear();
            }
            catch (Exception ex)
            {
                _processingException = ex;
                throw new SilverbackException("Failed to process batch. See inner exception for details.", ex);
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

        private async Task ProcessEachMessageAndPublishEvents(IReadOnlyCollection<IRawInboundEnvelope> envelopes)
        {
            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            try
            {
                await publisher.PublishAsync(new BatchCompleteEvent(CurrentBatchId, envelopes));
                await _messagesHandler(envelopes, scope.ServiceProvider);
                await publisher.PublishAsync(new BatchProcessedEvent(CurrentBatchId, envelopes));

                await _commitHandler.Invoke(envelopes, scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                await _rollbackHandler.Invoke(envelopes, scope.ServiceProvider);

                await publisher.PublishAsync(new BatchAbortedEvent(CurrentBatchId, envelopes, ex));

                throw;
            }
        }
    }
}