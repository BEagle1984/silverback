// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.ErrorHandling
{
    internal class ErrorPolicyHelper : IErrorPolicyHelper
    {
        private readonly ISilverbackIntegrationLogger<ErrorPolicyHelper> _logger;

        private readonly IServiceScopeFactory _serviceScopeFactory;

        public ErrorPolicyHelper(
            ISilverbackIntegrationLogger<ErrorPolicyHelper> logger,
            IServiceScopeFactory serviceScopeFactory)
        {
            _logger = logger;
            _serviceScopeFactory = serviceScopeFactory;
        }

        public async Task TryProcessAsync(
            ConsumerPipelineContext context,
            IErrorPolicy? errorPolicy,
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
                            attempt)
                        .ConfigureAwait(false);

                    if (result.IsSuccessful || result.Action == ErrorAction.Skip)
                    {
                        await commitHandler(context, scope.ServiceProvider).ConfigureAwait(false);
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
                    await rollbackHandler(context, scope.ServiceProvider, ex).ConfigureAwait(false);

                    throw;
                }
            }
        }

        private static int GetAttemptNumber(IReadOnlyCollection<IRawInboundEnvelope> envelopes)
        {
            var minAttempts = envelopes.Min(
                m =>
                    m.Headers.GetValueOrDefault<int>(DefaultMessageHeaders.FailedAttempts));

            // Uniform failed attempts, just in case (mostly for consistent logging)
            UpdateFailedAttemptsHeader(envelopes, minAttempts);

            return minAttempts + 1;
        }

        private static void UpdateFailedAttemptsHeader(
            IReadOnlyCollection<IRawInboundEnvelope> envelopes,
            int attempt) =>
            envelopes?.ForEach(
                msg =>
                {
                    if (attempt == 0)
                        msg.Headers.Remove(DefaultMessageHeaders.FailedAttempts);
                    else
                        msg.Headers.AddOrReplace(DefaultMessageHeaders.FailedAttempts, attempt);
                });

        private async Task<MessageHandlerResult> HandleMessages(
            ConsumerPipelineContext context,
            IServiceProvider serviceProvider,
            ConsumerBehaviorHandler messagesHandler,
            ConsumerBehaviorErrorHandler rollbackHandler,
            IErrorPolicy? errorPolicy,
            int attempt)
        {
            try
            {
                _logger.LogProcessing(context.Envelopes);

                await messagesHandler(context, serviceProvider).ConfigureAwait(false);

                return MessageHandlerResult.Success;
            }
            catch (Exception ex)
            {
                _logger.LogProcessingError(context.Envelopes, ex);

                if (errorPolicy == null)
                    throw;

                UpdateFailedAttemptsHeader(context.Envelopes, attempt);

                if (!errorPolicy.CanHandle(context.Envelopes, ex))
                    throw;

                var action = await errorPolicy.HandleError(context.Envelopes, ex).ConfigureAwait(false);

                if (action == ErrorAction.StopConsuming)
                    throw;

                var offsets = context.CommitOffsets;

                // Rollback database transactions only (ignore offsets)
                context.CommitOffsets = new List<IOffset>();
                await rollbackHandler(context, serviceProvider, ex).ConfigureAwait(false);

                if (action == ErrorAction.Skip)
                {
                    // Reset offsets to always commit them even in case of Skip
                    context.CommitOffsets = offsets;
                }

                return MessageHandlerResult.Error(action);
            }
        }
    }
}
