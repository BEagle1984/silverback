// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Inbound.ErrorHandling
{
    /// <summary>
    ///     This policy skips the message that failed to be processed.
    /// </summary>
    public class SkipMessageErrorPolicy : ErrorPolicyBase
    {
        /// <inheritdoc cref="ErrorPolicyBase.BuildCore" />
        protected override ErrorPolicyImplementation BuildCore(IServiceProvider serviceProvider) =>
            new SkipMessageErrorPolicyImplementation(
                MaxFailedAttemptsCount,
                ExcludedExceptions,
                IncludedExceptions,
                ApplyRule,
                MessageToPublishFactory,
                serviceProvider,
                serviceProvider.GetRequiredService<ISilverbackIntegrationLogger<SkipMessageErrorPolicy>>());

        private class SkipMessageErrorPolicyImplementation : ErrorPolicyImplementation
        {
            private readonly ISilverbackIntegrationLogger<SkipMessageErrorPolicy> _logger;

            public SkipMessageErrorPolicyImplementation(
                int? maxFailedAttempts,
                ICollection<Type> excludedExceptions,
                ICollection<Type> includedExceptions,
                Func<IRawInboundEnvelope, Exception, bool>? applyRule,
                Func<IRawInboundEnvelope, object>? messageToPublishFactory,
                IServiceProvider serviceProvider,
                ISilverbackIntegrationLogger<SkipMessageErrorPolicy> logger)
                : base(
                    maxFailedAttempts,
                    excludedExceptions,
                    includedExceptions,
                    applyRule,
                    messageToPublishFactory,
                    serviceProvider,
                    logger)
            {
                _logger = logger;
            }

            protected override async Task<bool> ApplyPolicyAsync(ConsumerPipelineContext context, Exception exception)
            {
                Check.NotNull(context, nameof(context));
                Check.NotNull(exception, nameof(exception));

                _logger.LogWithMessageInfo(
                    LogLevel.Error,
                    IntegrationEventIds.MessageSkipped,
                    exception,
                    "The message(s) will be skipped.",
                    context);

                await context.TransactionManager.RollbackAsync(exception, true).ConfigureAwait(false);

                return true;
            }
        }
    }
}
