// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.ErrorHandling
{
    // TODO: Test
    public class ErrorPolicyHelper
    {
        private readonly ILogger _logger;
        private readonly MessageLogger _messageLogger;

        public ErrorPolicyHelper(MessageLogger messageLogger, ILogger<ErrorPolicyHelper> logger)
        {
            _messageLogger = messageLogger;
            _logger = logger;
        }

        public void TryProcessMessage<TMessage>(IErrorPolicy errorPolicy, TMessage message, Action<TMessage> messageHandler)
        {
            int attempts = 1;

            while (true)
            {
                var result = HandleMessage(message, messageHandler, attempts, errorPolicy);

                if (result.IsSuccessful || result.Action == ErrorAction.Skip)
                    return;

                attempts++;
            }
        }

        private MessageHandlerResult HandleMessage<TMessage>(TMessage message, Action<TMessage> messageHandler, int failedAttempts,
            IErrorPolicy errorPolicy)
        {
            try
            {
                messageHandler(message);

                return MessageHandlerResult.Success;
            }
            catch (Exception ex)
            {
                if (errorPolicy == null)
                    throw;

                var failedMessage = new FailedMessage(message, failedAttempts);

                if (!errorPolicy.CanHandle(failedMessage, ex))
                    throw;

                var action = errorPolicy.HandleError(failedMessage, ex);

                if (action == ErrorAction.StopConsuming)
                    throw;

                return MessageHandlerResult.Error(action);
            }
        }
    }
}
