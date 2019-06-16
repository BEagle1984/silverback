// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
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
        private readonly InboundMessageProcessor _inboundMessageProcessor;

        private readonly Action<IEnumerable<IInboundMessage>, IServiceProvider> _messagesHandler;
        private readonly Action<IEnumerable<IOffset>, IServiceProvider> _commitHandler;
        private readonly Action<IServiceProvider> _rollbackHandler;

        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger _logger;
        private readonly MessageLogger _messageLogger;

        private readonly List<IInboundMessage> _messages;
        private readonly Timer _waitTimer;

        private Exception _processingException;

        public MessageBatch(IEndpoint endpoint,
            BatchSettings settings,
            Action<IEnumerable<IInboundMessage>, IServiceProvider> messagesHandler,
            Action<IEnumerable<IOffset>, IServiceProvider> commitHandler,
            Action<IServiceProvider> rollbackHandler,
            IErrorPolicy errorPolicy, 
            IServiceProvider serviceProvider)
        {
            _endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

            _messagesHandler = messagesHandler ?? throw new ArgumentNullException(nameof(messagesHandler));
            _commitHandler = commitHandler ?? throw new ArgumentNullException(nameof(commitHandler));
            _rollbackHandler = rollbackHandler ?? throw new ArgumentNullException(nameof(rollbackHandler));

            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _inboundMessageProcessor = serviceProvider.GetRequiredService<InboundMessageProcessor>();

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

        public void AddMessage(IInboundMessage message)
        {
            // TODO: Check this!
            if (_processingException != null)
                throw new SilverbackException("Cannot add to the batch because the processing of the previous batch failed. See inner exception for details.", _processingException);

            lock (_messages)
            {
                _messages.Add(message);

                _messageLogger.LogInformation(_logger, "Message added to batch.", message, CurrentBatchId, CurrentSize);

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
                _inboundMessageProcessor.TryDeserializeAndProcess(
                    new InboundBatch(CurrentBatchId, _messages, _endpoint),
                    _errorPolicy,
                    deserializedBatch => ProcessEachMessageAndPublishEvents((IInboundBatch) deserializedBatch));

                _messages.Clear();
            }
            catch (Exception ex)
            {
                _processingException = ex;
                throw new SilverbackException("Failed to process batch. See inner exception for details.", ex);
            }
        }

        private void ProcessEachMessageAndPublishEvents(IInboundBatch deserializedBatch)
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var publisher = scope.ServiceProvider.GetRequiredService<IPublisher>();

                var unwrappedMessages = deserializedBatch.Messages.Select(m => m.Message).ToList();

                try
                {
                    publisher.Publish(new BatchCompleteEvent(CurrentBatchId, unwrappedMessages));
                    _messagesHandler(deserializedBatch.Messages, scope.ServiceProvider);
                    publisher.Publish(new BatchProcessedEvent(CurrentBatchId, unwrappedMessages));

                    _commitHandler?.Invoke(_messages.Select(m => m.Offset).ToList(), scope.ServiceProvider);
                }
                catch (Exception ex)
                {
                    _rollbackHandler?.Invoke(scope.ServiceProvider);

                    publisher.Publish(new BatchAbortedEvent(CurrentBatchId, unwrappedMessages, ex));

                    throw;
                }
            }
        }
    }
}
