// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
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

        public async Task TryProcessAsync(
            IEnumerable<IInboundMessage> messages,
            IErrorPolicy errorPolicy,
            Func<IEnumerable<IInboundMessage>, Task> messagesHandler)
        {
            var attempt = GetAttemptNumber(messages);

            while (true)
            {
                var result = await HandleMessages(messages, messagesHandler, errorPolicy, attempt);

                if (result.IsSuccessful || result.Action == ErrorAction.Skip)
                    return;

                attempt++;
            }
        }

        private int GetAttemptNumber(IEnumerable<IInboundMessage> messages)
        {
            var minAttempts = messages.Min(m => m.Headers.GetValueOrDefault<int>(MessageHeader.FailedAttemptsKey));

            // Uniform failed attempts, just in case (mostly for consistent logging)
            UpdateFailedAttemptsHeader(messages, minAttempts);

            return minAttempts + 1;
        }

        private async Task<MessageHandlerResult> HandleMessages(
            IEnumerable<IInboundMessage> messages,
            Func<IEnumerable<IInboundMessage>, Task> messagesHandler,
            IErrorPolicy errorPolicy,
            int attempt)
        {
            try
            {
                _messageLogger.LogProcessing(_logger, messages);

                await messagesHandler(messages);

                return MessageHandlerResult.Success;
            }
            catch (Exception ex)
            {
                _messageLogger.LogProcessingError(_logger, messages, ex);

                if (errorPolicy == null)
                    throw;

                UpdateFailedAttemptsHeader(messages, attempt);

                if (!errorPolicy.CanHandle(messages, ex))
                    throw;

                var action = errorPolicy.HandleError(messages, ex);

                if (action == ErrorAction.StopConsuming)
                    throw;

                return MessageHandlerResult.Error(action);
            }
        }

        private void UpdateFailedAttemptsHeader(IEnumerable<IBrokerMessage> messages, int attempt) =>
            messages?.ForEach(msg =>
            {
                if (attempt == 0)
                    msg.Headers.Remove(MessageHeader.FailedAttemptsKey);
                else
                    msg.Headers.AddOrReplace(MessageHeader.FailedAttemptsKey, attempt);
            });
    }
}