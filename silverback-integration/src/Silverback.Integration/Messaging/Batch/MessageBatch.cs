using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

namespace Silverback.Messaging.Batch
{
    // TODO: Test
    public class MessageBatch
    {
        private readonly ILogger _logger;
        private readonly List<IMessage> _messages;
        private readonly Timer _waitTimer;
        private readonly Action<IMessage, IEndpoint, IServiceProvider> _messageHandler;
        private readonly Action<IServiceProvider> _commitHandler;
        private readonly Action<IServiceProvider> _errorHandler;
        private readonly IServiceProvider _serviceProvider;
        private readonly IErrorPolicy _errorPolicy;
        private readonly IPublisher _publisher;
        private Exception _processingException;

        public MessageBatch(IEndpoint endpoint, 
            Action<IMessage, IEndpoint, IServiceProvider> messageHandler, 
            Action<IServiceProvider> commitHandler, 
            Action<IServiceProvider> errorHandler, 
            IErrorPolicy errorPolicy, IServiceProvider serviceProvider, ILogger<MessageBatch> logger)
        {
            Endpoint = endpoint ?? throw new ArgumentNullException(nameof(endpoint));

            _messageHandler = messageHandler ?? throw new ArgumentNullException(nameof(messageHandler));
            _commitHandler = commitHandler ?? throw new ArgumentNullException(nameof(commitHandler));
            _errorHandler = errorHandler ?? throw new ArgumentNullException(nameof(errorHandler));

            _errorPolicy = errorPolicy;

            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _logger = logger;

            _messages = new List<IMessage>(endpoint.Batch.Size);

            if (Endpoint.Batch.MaxWaitTime < TimeSpan.MaxValue)
            {
                _waitTimer = new Timer(Endpoint.Batch.MaxWaitTime.TotalMilliseconds);
                _waitTimer.Elapsed += OnWaitTimerElapsed;
            }

            _publisher = serviceProvider.GetRequiredService<IPublisher>();
        }

        public IEndpoint Endpoint { get; }

        public Guid CurrentBatchId { get; private set; }

        public int CurrentSize => _messages.Count;

        public void AddMessage(IMessage message)
        {
            if (_processingException != null)
                throw new SilverbackException("Cannot add to the batch because the processing of the previous batch failed. See inner exception for details.", _processingException);

            lock (_messages)
            {
                _messages.Add(message);

                _logger.LogTrace("Message added to batch.", message, Endpoint, this);

                if (_messages.Count == 1)
                {
                    CurrentBatchId = Guid.NewGuid();
                    _waitTimer?.Start();
                }
                else if (_messages.Count == Endpoint.Batch.Size)
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

                var batchReadyEvent = new BatchProcessingMessage(CurrentBatchId, _messages);
                _errorPolicy.TryProcess(
                    batchReadyEvent,
                    _ => ProcessEachMessageAndPublishEvents());

                _messages.Clear();
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
                    _publisher.Publish(new BatchReadyEvent(CurrentBatchId, _messages));

                    Parallel.ForEach(
                        _messages,
                        new ParallelOptions { MaxDegreeOfParallelism = Endpoint.Batch.MaxDegreeOfParallelism },
                        message => _messageHandler(message, Endpoint, scope.ServiceProvider));

                    _publisher.Publish(new BatchProcessedEvent(CurrentBatchId, _messages));

                    _commitHandler?.Invoke(scope.ServiceProvider);
                }
                catch (Exception)
                {
                    _errorHandler?.Invoke(scope.ServiceProvider);
                    throw;
                }
            }
        }
    }
}
