// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Batch
{
    // TODO: Test? (or implicitly tested with InboundConnector?)
    public class MessageBatch
    {
        private readonly IEndpoint _endpoint;
        private readonly BatchSettings _settings;
        private readonly IErrorPolicy _errorPolicy;

        private readonly Action<IMessage, IEndpoint, IServiceProvider> _messageHandler;
        private readonly Action<IEnumerable<IOffset>, IServiceProvider> _commitHandler;
        private readonly Action<IServiceProvider> _rollbackHandler;

        private readonly IServiceProvider _serviceProvider;
        private readonly IPublisher _publisher;
        private readonly ILogger _logger;

        private readonly List<IMessage> _messages;
        private readonly List<IOffset> _offsets;
        private readonly Timer _waitTimer;

        private Exception _processingException;

        public MessageBatch(IEndpoint endpoint,
            BatchSettings settings,
            Action<IMessage, IEndpoint, IServiceProvider> messageHandler,
            Action<IEnumerable<IOffset>, IServiceProvider> commitHandler,
            Action<IServiceProvider> rollbackHandler,
            IErrorPolicy errorPolicy, 
            IServiceProvider serviceProvider)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

            _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
            _commitHandler = commitHandler ?? throw new ArgumentNullException(nameof(commitHandler));
            _rollbackHandler = rollbackHandler ?? throw new ArgumentNullException(nameof(rollbackHandler));

            _errorPolicy = errorPolicy;

            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _settings = settings;

            _messages = new List<IMessage>(_settings.Size);
            _offsets = new List<IOffset>(_settings.Size);

            if (_settings.MaxWaitTime < TimeSpan.MaxValue)
            {
                _waitTimer = new Timer(_settings.MaxWaitTime.TotalMilliseconds);
                _waitTimer.Elapsed += OnWaitTimerElapsed;
            }

            _publisher = serviceProvider.GetRequiredService<IPublisher>();
            _logger = serviceProvider.GetRequiredService<ILogger<MessageBatch>>();
        }

        public Guid CurrentBatchId { get; private set; }

        public int CurrentSize => _messages.Count;

        public void AddMessage(IMessage message, IOffset offset)
        {
            if (_processingException != null)
                throw new SilverbackException("Cannot add to the batch because the processing of the previous batch failed. See inner exception for details.", _processingException);

            lock (_messages)
            {
                _messages.Add(message);
                _offsets.Add(offset);

                _logger.LogTrace("Message added to batch.", message, _endpoint, this);

                if (_messages.Count == 1)
                {
                    CurrentBatchId = Guid.NewGuid();
                    _waitTimer?.Start();
                }
                else if (_messages.Count == _settings.Size)
                {
                    ProcessBatch();
                }
            }
        }

        private void OnWaitTimerElapsed(object sender, ElapsedEventArgs e)
        {
            lock (_messages)
            {
                _waitTimer?.Stop();

                if (_messages.Any())
                    ProcessBatch();
            }
        }

        private void ProcessBatch()
        {
            try
            {
                _logger.LogTrace("Processing batch '{batchId}' containing {batchSize}.", CurrentBatchId, _messages.Count);

                _errorPolicy.TryProcess(
                    new BatchCompleteEvent(CurrentBatchId, _messages),
                    _ => ProcessEachMessageAndPublishEvents());

                _messages.Clear();
                _offsets.Clear();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process batch '{batchId}' containing {batchSize}.", CurrentBatchId, _messages.Count);

                _processingException = ex;
                throw new SilverbackException("Failed to process batch. See inner exception for details.", ex);
            }
        }

        private void ProcessEachMessageAndPublishEvents()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                try
                {
                    _publisher.Publish(new BatchCompleteEvent(CurrentBatchId, _messages));

                    Parallel.ForEach(
                        _messages,
                        new ParallelOptions { MaxDegreeOfParallelism = _settings.MaxDegreeOfParallelism },
                        message => _messageHandler(message, _endpoint, scope.ServiceProvider));

                    _publisher.Publish(new BatchProcessedEvent(CurrentBatchId, _messages));

                    _commitHandler?.Invoke(_offsets, scope.ServiceProvider);
                }
                catch (Exception)
                {
                    _rollbackHandler?.Invoke(scope.ServiceProvider);

                    _publisher.Publish(new BatchAbortedEvent(CurrentBatchId, _messages));

                    throw;
                }
            }
        }
    }
}
