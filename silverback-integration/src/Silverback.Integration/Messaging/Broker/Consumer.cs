// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.ErrorHandling;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Broker
{
    public abstract class Consumer : EndpointConnectedObject, IConsumer
    {
        private readonly ILogger<Consumer> _logger;

        protected Consumer(IBroker broker, IEndpoint endpoint, ILogger<Consumer> logger)
           : base(broker, endpoint)
        {
            _logger = logger;
        }

        public event EventHandler<IMessage> Received;
        public event EventHandler<ErrorHandlerEventArgs> Error;

        protected MessageHandlerResult HandleMessage(byte[] buffer, int retryCount)
        {
            if (Received == null)
                throw new InvalidOperationException("A message was received but no handler is configured, please attach to the Received event.");

            IMessage message = null;

            try
            {
                message = DeserializeMessage(buffer);

                _logger.LogTrace("Message received.", message, Endpoint);

                RaiseReceivedEvent(message);

                return MessageHandlerResult.Success;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error occurred processing the message.", message, Endpoint);

                var errorArgs = new ErrorHandlerEventArgs(ex, GetFailedMessage(message));
                Error?.Invoke(this, errorArgs);

                return MessageHandlerResult.Error(errorArgs.Action);
            }
        }
        private IMessage DeserializeMessage(byte[] buffer) => Endpoint.Serializer.Deserialize(buffer);

        private void RaiseReceivedEvent(IMessage message)
        {
            if (message is FailedMessage failedMessage)
            {
                Received.Invoke(this, failedMessage.Message);
            }
            else
            {
                Received.Invoke(this, message);
            }
        }

        private FailedMessage GetFailedMessage(IMessage message)
        {
            if (message is FailedMessage failedMessage)
            {
                failedMessage.FailedAttempts++;
                return failedMessage;
            }

            return new FailedMessage(message);
        }

    }

    public abstract class Consumer<TBroker, TEndpoint> : Consumer
        where TBroker : class, IBroker
        where TEndpoint : class, IEndpoint
    {
        protected Consumer(IBroker broker, IEndpoint endpoint, ILogger<Consumer> logger) 
            : base(broker, endpoint, logger)
        {
        }

        protected new TBroker Broker => (TBroker)base.Broker;

        protected new TEndpoint Endpoint => (TEndpoint)base.Endpoint;
    }
}