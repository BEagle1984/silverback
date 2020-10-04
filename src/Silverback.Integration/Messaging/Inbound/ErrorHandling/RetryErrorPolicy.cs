// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Inbound.ErrorHandling
{
    /// <summary>
    ///     This policy retries the handler method multiple times in case of exception. An optional delay can be
    ///     specified.
    /// </summary>
    /// TODO: Exponential backoff variant
    public class RetryErrorPolicy : ErrorPolicyBase
    {
        private readonly TimeSpan _initialDelay;

        private readonly TimeSpan _delayIncrement;

        /// <summary>
        ///     Initializes a new instance of the <see cref="RetryErrorPolicy" /> class.
        /// </summary>
        /// <param name="initialDelay">
        ///     The optional delay to be applied to the first retry.
        /// </param>
        /// <param name="delayIncrement">
        ///     The optional increment to the delay to be applied at each retry.
        /// </param>
        public RetryErrorPolicy(TimeSpan? initialDelay = null, TimeSpan? delayIncrement = null)
        {
            _initialDelay = initialDelay ?? TimeSpan.Zero;
            _delayIncrement = delayIncrement ?? TimeSpan.Zero;

            if (_initialDelay < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(initialDelay),
                    initialDelay,
                    "The specified initial delay must be greater than TimeSpan.Zero.");
            }

            if (_delayIncrement < TimeSpan.Zero)
            {
                throw new ArgumentOutOfRangeException(
                    nameof(delayIncrement),
                    delayIncrement,
                    "The specified delay increment must be greater than TimeSpan.Zero.");
            }
        }

        /// <inheritdoc cref="ErrorPolicyBase.BuildCore" />
        protected override ErrorPolicyImplementation BuildCore(IServiceProvider serviceProvider) =>
            new RetryErrorPolicyImplementation(
                _initialDelay,
                _delayIncrement,
                MaxFailedAttemptsCount,
                ExcludedExceptions,
                IncludedExceptions,
                ApplyRule,
                MessageToPublishFactory,
                serviceProvider,
                serviceProvider.GetRequiredService<ISilverbackIntegrationLogger<RetryErrorPolicy>>());

        private class RetryErrorPolicyImplementation : ErrorPolicyImplementation
        {
            private readonly TimeSpan _initialDelay;

            private readonly TimeSpan _delayIncrement;

            private readonly ISilverbackIntegrationLogger<RetryErrorPolicy> _logger;

            public RetryErrorPolicyImplementation(
                TimeSpan initialDelay,
                TimeSpan delayIncrement,
                int? maxFailedAttempts,
                ICollection<Type> excludedExceptions,
                ICollection<Type> includedExceptions,
                Func<IRawInboundEnvelope, Exception, bool>? applyRule,
                Func<IRawInboundEnvelope, object>? messageToPublishFactory,
                IServiceProvider serviceProvider,
                ISilverbackIntegrationLogger<RetryErrorPolicy> logger)
                : base(
                    maxFailedAttempts,
                    excludedExceptions,
                    includedExceptions,
                    applyRule,
                    messageToPublishFactory,
                    serviceProvider,
                    logger)
            {
                _initialDelay = initialDelay;
                _delayIncrement = delayIncrement;
                _logger = logger;
            }

            protected override async Task<bool> ApplyPolicy(ConsumerPipelineContext context, Exception exception)
            {
                Check.NotNull(context, nameof(context));
                Check.NotNull(exception, nameof(exception));

                await context.TransactionManager.Rollback(exception).ConfigureAwait(false);

                await ApplyDelay(context.Envelope).ConfigureAwait(false);

                _logger.LogInformationWithMessageInfo(
                    IntegrationEventIds.RetryMessageProcessing,
                    "The message(s) will be processed again.",
                    context.Envelope);

                return true;
            }

            private async Task ApplyDelay(IRawInboundEnvelope envelope)
            {
                var delay = (int)_initialDelay.TotalMilliseconds +
                            (envelope.Headers.GetValueOrDefault<int>(DefaultMessageHeaders.FailedAttempts) *
                             (int)_delayIncrement.TotalMilliseconds);

                if (delay <= 0)
                    return;

                _logger.LogTraceWithMessageInfo(
                    IntegrationEventIds.RetryDelayed,
                    $"Waiting {delay} milliseconds before retrying to process the message(s).",
                    envelope);

                await Task.Delay(delay).ConfigureAwait(false);
            }
        }
    }
}
