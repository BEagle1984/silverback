// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Consuming.ErrorHandling;

/// <summary>
///     This policy skips the message that failed to be processed.
/// </summary>
public record SkipMessageErrorPolicy : ErrorPolicyBase
{
    internal SkipMessageErrorPolicy()
    {
    }

    /// <inheritdoc cref="ErrorPolicyBase.BuildCore" />
    protected override ErrorPolicyImplementation BuildCore(IServiceProvider serviceProvider) =>
        new SkipMessageErrorPolicyImplementation(
            MaxFailedAttempts,
            ExcludedExceptions,
            IncludedExceptions,
            ApplyRule,
            MessageToPublishFactory,
            serviceProvider,
            serviceProvider.GetRequiredService<IConsumerLogger<SkipMessageErrorPolicy>>());

    private sealed class SkipMessageErrorPolicyImplementation : ErrorPolicyImplementation
    {
        private readonly IConsumerLogger<SkipMessageErrorPolicy> _logger;

        public SkipMessageErrorPolicyImplementation(
            int? maxFailedAttempts,
            IReadOnlyCollection<Type> excludedExceptions,
            IReadOnlyCollection<Type> includedExceptions,
            Func<IRawInboundEnvelope, Exception, bool>? applyRule,
            Func<IRawInboundEnvelope, Exception, object?>? messageToPublishFactory,
            IServiceProvider serviceProvider,
            IConsumerLogger<SkipMessageErrorPolicy> logger)
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

        protected override async Task<bool> ApplyPolicyAsync(
            ConsumerPipelineContext context,
            Exception exception)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(exception, nameof(exception));

            _logger.LogSkipped(context.Envelope);

            if (!await TryRollbackAsync(context, exception).ConfigureAwait(false))
                await context.Consumer.TriggerReconnectAsync().ConfigureAwait(false);

            return true;
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task<bool> TryRollbackAsync(ConsumerPipelineContext context, Exception exception)
        {
            try
            {
                await context.TransactionManager.RollbackAsync(exception, true)
                    .ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogRollbackToSkipFailed(context.Envelope, ex);
                return false;
            }
        }
    }
}
