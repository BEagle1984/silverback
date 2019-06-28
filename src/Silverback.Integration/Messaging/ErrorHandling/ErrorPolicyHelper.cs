// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.ErrorHandling
{
    // TODO: Test
    public class ErrorPolicyHelper
    {
        private readonly ILogger<ErrorPolicyHelper> _logger;
        private readonly MessageLogger _messageLogger;

        public ErrorPolicyHelper(ILogger<ErrorPolicyHelper> logger, MessageLogger messageLogger)
        {
            _logger = logger;
            _messageLogger = messageLogger;
        }

        public void TryProcess(
            IEnumerable<IRawInboundMessage> messages, 
            IErrorPolicy errorPolicy,
            Action<IEnumerable<IRawInboundMessage>> messagesHandler)
        {
            var attempt = messages.Min(m => m.Headers.GetValue<int>(MessageHeader.FailedAttemptsKey)) + 1;

            while (true)
            {
                var result = HandleMessages(messages, messagesHandler, errorPolicy, attempt);

                if (result.IsSuccessful || result.Action == ErrorAction.Skip)
                    return;

                attempt++;
            }
        }

        private MessageHandlerResult HandleMessages(
            IEnumerable<IRawInboundMessage> messages,
            Action<IEnumerable<IRawInboundMessage>> messagesHandler, 
            IErrorPolicy errorPolicy, int attempt)
        {
            try
            {
                _messageLogger.LogProcessing(_logger, messages);

                messagesHandler(messages);

                return MessageHandlerResult.Success;
            }
            catch (Exception ex)
            {
                _messageLogger.LogProcessingError(_logger, messages, ex);

                if (errorPolicy == null)
                    throw;

                UpdateFailedAttemptsHeader(messages, attempt);

                if (messages.Any(m => !errorPolicy.CanHandle(m, ex)))
                    throw;

                var action = errorPolicy.HandleError(messages, ex);

                if (action == ErrorAction.StopConsuming)
                    throw;

                return MessageHandlerResult.Error(action);
            }
        }

        private void UpdateFailedAttemptsHeader(IEnumerable<IInboundMessage> messages, int attempt) => 
            messages.ForEach(msg => msg.Headers.AddOrReplace(MessageHeader.FailedAttemptsKey, attempt));
    }
}
