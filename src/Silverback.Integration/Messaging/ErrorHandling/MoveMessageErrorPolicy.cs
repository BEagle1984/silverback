// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    /// <summary>
    /// This policy moves the failed messages to the configured endpoint.
    /// </summary>
    public class MoveMessageErrorPolicy : ErrorPolicyBase
    {
        private readonly IProducer _producer;
        private readonly IEndpoint _endpoint;
        private readonly ILogger _logger;
        private readonly MessageLogger _messageLogger;

        private Func<object, Exception, object> _transformationFunction;
        private Func<MessageHeaderCollection, Exception, MessageHeaderCollection> _headersTransformationFunction;

        public MoveMessageErrorPolicy(IBroker broker, IEndpoint endpoint, IServiceProvider serviceProvider, ILogger<MoveMessageErrorPolicy> logger, MessageLogger messageLogger) 
            : base(serviceProvider, logger, messageLogger)
        {
            if (broker == null) throw new ArgumentNullException(nameof(broker));
            if (endpoint == null) throw new ArgumentNullException(nameof(endpoint));
            if (serviceProvider == null) throw new ArgumentNullException(nameof(serviceProvider));

            _producer = broker.GetProducer(endpoint);
            _endpoint = endpoint;
            _logger = logger;
            _messageLogger = messageLogger;
        }

        public MoveMessageErrorPolicy Transform(Func<object, Exception, object> transformationFunction,
            Func<MessageHeaderCollection, Exception, MessageHeaderCollection> headersTransformationFunction = null)
        {
            _transformationFunction = transformationFunction;
            _headersTransformationFunction = headersTransformationFunction;
            return this;
        }

        protected override ErrorAction ApplyPolicy(IInboundMessage message, Exception exception)
        {
            _messageLogger.LogInformation(_logger, $"The message will be  be moved to '{_endpoint.Name}'.", message);

            if (message is IInboundBatch inboundBatch)
            {
                foreach (var singleFailedMessage in inboundBatch.Messages)
                {
                    PublishToNewEndpoint(singleFailedMessage, exception);
                }
            }
            else
            {
                PublishToNewEndpoint(message, exception);
            }

            return ErrorAction.Skip;
        }

        private void PublishToNewEndpoint(IInboundMessage message, Exception exception)
        {
            _producer.Produce(
                _transformationFunction?.Invoke(message.Message, exception) ?? message.Message,
                _headersTransformationFunction?.Invoke(message.Headers, exception) ?? message.Headers);
        }
    }
}