// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using RabbitMQ.Client;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Broker
{
    public class RabbitProducer : Producer<RabbitBroker, RabbitProducerEndpoint>, IDisposable
    {
        private readonly ILogger<Producer> _logger;
        private readonly IModel _channel;
        
        private readonly BlockingCollection<QueuedMessage> _queue = new BlockingCollection<QueuedMessage>();
        
        private readonly CancellationTokenSource _cancellationTokenSource = new CancellationTokenSource();
        
        public RabbitProducer(RabbitBroker broker,
            RabbitProducerEndpoint endpoint,
            MessageKeyProvider messageKeyProvider,
            IEnumerable<IProducerBehavior> behaviors,
            RabbitConnectionFactory connectionFactory,
            ILogger<Producer> logger,
            MessageLogger messageLogger) 
            : base(broker, endpoint, messageKeyProvider, behaviors, logger, messageLogger)
        {
            _logger = logger;

            _channel = connectionFactory.GetChannel(endpoint);

            Task.Run(() => ProcessQueue(_cancellationTokenSource.Token));
        }

        protected override IOffset Produce(RawBrokerMessage message)=>
            AsyncHelper.RunSynchronously(() => ProduceAsync(message));

        protected override Task<IOffset> ProduceAsync(RawBrokerMessage message)
        {
            var queuedMessage = new QueuedMessage(message);

            _queue.Add(queuedMessage);
            
            return queuedMessage.TaskCompletionSource.Task;
        }

        private void ProcessQueue(CancellationToken cancellationToken)
        {
            try
            {
                while (!cancellationToken.IsCancellationRequested)
                {
                    var queuedMessage = _queue.Take(cancellationToken);

                    try
                    {
                        PublishToChannel(queuedMessage.Message);
                        
                        queuedMessage.TaskCompletionSource.SetResult(null);
                    }
                    catch (Exception ex)
                    {
                        queuedMessage.TaskCompletionSource.SetException(ex);
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                _logger.LogDebug(ex, "Producer queue processing was cancelled.");
            }
        }
        
        private void PublishToChannel(RawBrokerMessage message)
        {
            var endpoint = (RabbitProducerEndpoint) message.Endpoint;

            var properties = _channel.CreateBasicProperties();
            properties.Persistent = true; // TODO: Make it configurable
            properties.Headers = message.Headers.ToDictionary(header => header.Key, header => (object) header.Value);

            switch (Endpoint)
            {
                case RabbitQueueProducerEndpoint queueEndpoint:
                    _channel.BasicPublish("", queueEndpoint.Name, properties, message.RawContent);
                    break;
                case RabbitExchangeProducerEndpoint exchangeEndpoint:
                    _channel.BasicPublish(exchangeEndpoint.Name, "", // TODO: Routing key from attribute
                        properties, message.RawContent);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            if (endpoint.ConfirmationTimeout.HasValue) _channel.WaitForConfirmsOrDie(endpoint.ConfirmationTimeout.Value);
        }

        private void Flush()
        {
            _queue.CompleteAdding();
            
            while (!_queue.IsCompleted)
                Task.Delay(100).Wait();
        }

        public void Dispose()
        {
            Flush();

            _cancellationTokenSource.Cancel();
            
            _channel?.Dispose();
        }

        private class QueuedMessage
        {
            public QueuedMessage(RawBrokerMessage message)
            {
                Message = message;
                TaskCompletionSource = new TaskCompletionSource<IOffset>();
            }

            public RawBrokerMessage Message { get; }
            public TaskCompletionSource<IOffset> TaskCompletionSource { get; }
        }
    }
}