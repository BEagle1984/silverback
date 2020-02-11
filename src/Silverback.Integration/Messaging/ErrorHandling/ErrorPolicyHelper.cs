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
            IReadOnlyCollection<IInboundEnvelope> envelopes,
            IErrorPolicy errorPolicy,
            Func<IReadOnlyCollection<IInboundEnvelope>, Task> messagesHandler)
        {
            var attempt = GetAttemptNumber(envelopes);

            while (true)
            {
                var result = await HandleMessages(envelopes, messagesHandler, errorPolicy, attempt);

                if (result.IsSuccessful || result.Action == ErrorAction.Skip)
                    return;

                attempt++;
            }
        }

        private int GetAttemptNumber(IReadOnlyCollection<IInboundEnvelope> envelopes)
        {
            var minAttempts = envelopes.Min(m => 
                m.Headers.GetValueOrDefault<int>(DefaultMessageHeaders.FailedAttempts));

            // Uniform failed attempts, just in case (mostly for consistent logging)
            UpdateFailedAttemptsHeader(envelopes, minAttempts);

            return minAttempts + 1;
        }

        private async Task<MessageHandlerResult> HandleMessages(
            IReadOnlyCollection<IInboundEnvelope> envelopes,
            Func<IReadOnlyCollection<IInboundEnvelope>, Task> messagesHandler,
            IErrorPolicy errorPolicy,
            int attempt)
        {
            try
            {
                _messageLogger.LogProcessing(_logger, envelopes);

                await messagesHandler(envelopes);

                return MessageHandlerResult.Success;
            }
            catch (Exception ex)
            {
                _messageLogger.LogProcessingError(_logger, envelopes, ex);

                if (errorPolicy == null)
                    throw;

                UpdateFailedAttemptsHeader(envelopes, attempt);

                if (!errorPolicy.CanHandle(envelopes, ex))
                    throw;

                var action = errorPolicy.HandleError(envelopes, ex);

                if (action == ErrorAction.StopConsuming)
                    throw;

                return MessageHandlerResult.Error(action);
            }
        }

        private void UpdateFailedAttemptsHeader(IReadOnlyCollection<IBrokerEnvelope> envelopes, int attempt) =>
            envelopes?.ForEach(msg =>
            {
                if (attempt == 0)
                    msg.Headers.Remove(DefaultMessageHeaders.FailedAttempts);
                else
                    msg.Headers.AddOrReplace(DefaultMessageHeaders.FailedAttempts, attempt);
            });
    }
}