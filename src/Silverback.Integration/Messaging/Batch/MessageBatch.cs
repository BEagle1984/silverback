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

        private readonly Func<IReadOnlyCollection<IInboundMessage>, IServiceProvider, Task> _messagesHandler;
        private readonly Func<IReadOnlyCollection<IOffset>, IServiceProvider, Task> _commitHandler;
        private readonly Func<IServiceProvider, Task> _rollbackHandler;

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly MessageLogger _messageLogger;

        private readonly List<IInboundMessage> _messages;
        private readonly Timer _waitTimer;
        private readonly SemaphoreSlim _semaphore = new SemaphoreSlim(1);

        private Exception _processingException;

        public MessageBatch(
            BatchSettings settings,
            Func<IReadOnlyCollection<IInboundMessage>, IServiceProvider, Task> messagesHandler,
            Func<IReadOnlyCollection<IOffset>, IServiceProvider, Task> commitHandler,
            Func<IServiceProvider, Task> rollbackHandler,
            IErrorPolicy errorPolicy,
            IServiceProvider serviceProvider)
        {
            _messagesHandler = messagesHandler ?? throw new ArgumentNullException(nameof(messagesHandler));
            _commitHandler = commitHandler ?? throw new ArgumentNullException(nameof(commitHandler));
            _rollbackHandler = rollbackHandler ?? throw new ArgumentNullException(nameof(rollbackHandler));

            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _errorPolicyHelper = serviceProvider.GetRequiredService<ErrorPolicyHelper>();

            _errorPolicy = errorPolicy;
            _settings = settings;

            _messages = new List<IInboundMessage>(_settings.Size);

            if (_settings.MaxWaitTime < TimeSpan.MaxValue)
            {
                _waitTimer = new Timer(_settings.MaxWaitTime.TotalMilliseconds);
                _waitTimer.Elapsed += OnWaitTimerElapsed;
            }

            _logger = serviceProvider.GetRequiredService<ILogger<MessageBatch>>();
            _messageLogger = serviceProvider.GetRequiredService<MessageLogger>();
        }

        public Guid CurrentBatchId { get; private set; }

        public int CurrentSize => _messages.Count;

        public async Task AddMessage(IInboundMessage message)
        {
            // TODO: Check this!
            if (_processingException != null)
                throw new SilverbackException(
                    "Cannot add to the batch because the processing of the previous batch failed. See inner exception for details.",
                    _processingException);

            await _semaphore.WaitAsync();

            try
            {
                _messages.Add(message);

                _messageLogger.LogInformation(_logger, "Message added to batch.", message);

                if (_messages.Count == 1)
                {
                    CurrentBatchId = Guid.NewGuid();
                    _waitTimer?.Start();
                }
                else if (_messages.Count == _settings.Size)
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
                        if (_messages.Any())
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
                AddHeaders(_messages);

                await _errorPolicyHelper.TryProcessAsync(
                    _messages,
                    _errorPolicy,
                    ProcessEachMessageAndPublishEvents);

                _messages.Clear();
            }
            catch (Exception ex)
            {
                _processingException = ex;
                throw new SilverbackException("Failed to process batch. See inner exception for details.", ex);
            }
        }

        private void AddHeaders(IReadOnlyCollection<IInboundMessage> messages)
        {
            foreach (var message in messages)
            {
                message.Headers.AddOrReplace(MessageHeader.BatchIdKey, CurrentBatchId);
                message.Headers.AddOrReplace(MessageHeader.BatchSizeKey, CurrentSize);
            }
        }

        private async Task ProcessEachMessageAndPublishEvents(IReadOnlyCollection<IInboundMessage> messages)
        {
            using var scope = _serviceProvider.CreateScope();
            var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

            try
            {
                await publisher.PublishAsync(new BatchCompleteEvent(CurrentBatchId, messages));
                await _messagesHandler(messages, scope.ServiceProvider);
                await publisher.PublishAsync(new BatchProcessedEvent(CurrentBatchId, messages));

                await _commitHandler.Invoke(_messages.Select(m => m.Offset).ToList(), scope.ServiceProvider);
            }
            catch (Exception ex)
            {
                await _rollbackHandler.Invoke(scope.ServiceProvider);

                await publisher.PublishAsync(new BatchAbortedEvent(CurrentBatchId, messages, ex));

                throw;
            }
        }
    }
}