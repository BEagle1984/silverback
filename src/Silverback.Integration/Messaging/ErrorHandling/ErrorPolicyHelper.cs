// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.ErrorHandling
{
    public class ErrorPolicyHelper
    {
        private readonly ILogger<ErrorPolicyHelper> _logger;
        private readonly MessageLogger _messageLogger;
        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ErrorPolicyHelper(
            ILogger<ErrorPolicyHelper> logger,
            MessageLogger messageLogger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _messageLogger = messageLogger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task TryProcessAsync(
            ConsumerPipelineContext context,
            IErrorPolicy errorPolicy,
            ConsumerBehaviorHandler messagesHandler,
            ConsumerBehaviorHandler commitHandler,
            ConsumerBehaviorErrorHandler rollbackHandler)
        {
            var attempt = GetAttemptNumber(context.Envelopes);
            var offsets = context.CommitOffsets;

            while (true)
            {
                using var scope = _serviceScopeFactory.CreateScope();

                try
                {
                    var result = await HandleMessages(
                        context,
                        scope.ServiceProvider,
                        messagesHandler,
                        rollbackHandler,
                        errorPolicy,
                        attempt);

                    if (result.IsSuccessful || result.Action == ErrorAction.Skip)
                    {
                        await commitHandler(context, scope.ServiceProvider);
                        return;
                    }

                    attempt++;

                    // Reset the offsets at each retry because they might have been modified
                    // (e.g. by the ChunkAggregatorConsumerBehavior) to handle commit and
                    // rollback and it's safer to rerun the pipeline with the very same state 
                    context.CommitOffsets = offsets;
                }
                catch (Exception ex)
                {
                    await rollbackHandler(context, scope.ServiceProvider, ex);

                    throw;
                }
            }
        }

        private int GetAttemptNumber(IReadOnlyCollection<IRawInboundEnvelope> envelopes)
        {
            var minAttempts = envelopes.Min(m =>
                m.Headers.GetValueOrDefault<int>(DefaultMessageHeaders.FailedAttempts));

            // Uniform failed attempts, just in case (mostly for consistent logging)
            UpdateFailedAttemptsHeader(envelopes, minAttempts);

            return minAttempts + 1;
        }

        private async Task<MessageHandlerResult> HandleMessages(
            ConsumerPipelineContext context,
            IServiceProvider serviceProvider,
            ConsumerBehaviorHandler messagesHandler,
            ConsumerBehaviorErrorHandler rollbackHandler,
            IErrorPolicy errorPolicy,
            int attempt)
        {
            try
            {
                _messageLogger.LogProcessing(_logger, context.Envelopes);

                await messagesHandler(context, serviceProvider);

                return MessageHandlerResult.Success;
            }
            catch (Exception ex)
            {
                _messageLogger.LogProcessingError(_logger, context.Envelopes, ex);

                if (errorPolicy == null)
                    throw;

                UpdateFailedAttemptsHeader(context.Envelopes, attempt);

                if (!errorPolicy.CanHandle(context.Envelopes, ex))
                    throw;

                var action = errorPolicy.HandleError(context.Envelopes, ex);

                if (action == ErrorAction.StopConsuming)
                    throw;

                var offsets = context.CommitOffsets;
                
                // Rollback database transactions only (ignore offsets)
                context.CommitOffsets = null;
                await rollbackHandler(context, serviceProvider, ex);

                if (action == ErrorAction.Skip)
                {
                    // Reset offsets to always commit them even in case of Skip
                    context.CommitOffsets = offsets;
                }

                return MessageHandlerResult.Error(action);
            }
        }

        private void UpdateFailedAttemptsHeader(IReadOnlyCollection<IRawInboundEnvelope> envelopes, int attempt) =>
            envelopes?.ForEach(msg =>
            {
                if (attempt == 0)
                    msg.Headers.Remove(DefaultMessageHeaders.FailedAttempts);
                else
                    msg.Headers.AddOrReplace(DefaultMessageHeaders.FailedAttempts, attempt);
            });
    }
}