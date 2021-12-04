// Copyright (c) 2020 Sergio Aquilini
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

namespace Silverback.Messaging.Inbound.ErrorHandling;

/// <summary>
///     This policy retries to process the message that previously failed to be to processed. An optional
///     delay can be specified.
/// </summary>
/// TODO: Exponential backoff variant
public class RetryErrorPolicy : RetryableErrorPolicyBase
{
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
        InitialDelay = initialDelay ?? TimeSpan.Zero;
        DelayIncrement = delayIncrement ?? TimeSpan.Zero;

        if (InitialDelay < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(initialDelay),
                initialDelay,
                "The specified initial delay must be greater than TimeSpan.Zero.");
        }

        if (DelayIncrement < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(
                nameof(delayIncrement),
                delayIncrement,
                "The specified delay increment must be greater than TimeSpan.Zero.");
        }
    }

    internal TimeSpan InitialDelay { get; }

    internal TimeSpan DelayIncrement { get; }

    /// <inheritdoc cref="ErrorPolicyBase.BuildCore" />
    protected override ErrorPolicyImplementation BuildCore(IServiceProvider serviceProvider) =>
        new RetryErrorPolicyImplementation(
            InitialDelay,
            DelayIncrement,
            MaxFailedAttemptsCount,
            ExcludedExceptions,
            IncludedExceptions,
            ApplyRule,
            MessageToPublishFactory,
            serviceProvider,
            serviceProvider.GetRequiredService<IInboundLogger<RetryErrorPolicy>>());

    private sealed class RetryErrorPolicyImplementation : ErrorPolicyImplementation
    {
        private readonly TimeSpan _initialDelay;

        private readonly TimeSpan _delayIncrement;

        private readonly IInboundLogger<RetryErrorPolicy> _logger;

        public RetryErrorPolicyImplementation(
            TimeSpan initialDelay,
            TimeSpan delayIncrement,
            int? maxFailedAttempts,
            ICollection<Type> excludedExceptions,
            ICollection<Type> includedExceptions,
            Func<IRawInboundEnvelope, Exception, bool>? applyRule,
            Func<IRawInboundEnvelope, Exception, object?>? messageToPublishFactory,
            IServiceProvider serviceProvider,
            IInboundLogger<RetryErrorPolicy> logger)
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

        protected override async Task<bool> ApplyPolicyAsync(
            ConsumerPipelineContext context,
            Exception exception)
        {
            Check.NotNull(context, nameof(context));
            Check.NotNull(exception, nameof(exception));

            if (!await TryRollbackAsync(context, exception).ConfigureAwait(false))
            {
                await context.Consumer.TriggerReconnectAsync().ConfigureAwait(false);
                return true;
            }

            await ApplyDelayAsync(context).ConfigureAwait(false);

            _logger.LogRetryProcessing(context.Envelope);

            return true;
        }

        [SuppressMessage("", "CA1031", Justification = Justifications.ExceptionLogged)]
        private async Task<bool> TryRollbackAsync(ConsumerPipelineContext context, Exception exception)
        {
            try
            {
                await context.TransactionManager.RollbackAsync(exception, stopConsuming: false)
                    .ConfigureAwait(false);

                return true;
            }
            catch (Exception ex)
            {
                _logger.LogRollbackToRetryFailed(context.Envelope, ex);
                return false;
            }
        }

        private async Task ApplyDelayAsync(ConsumerPipelineContext context)
        {
            int delay = (int)_initialDelay.TotalMilliseconds +
                        (context.Envelope.Headers.GetValueOrDefault<int>(DefaultMessageHeaders.FailedAttempts) *
                         (int)_delayIncrement.TotalMilliseconds);

            if (delay <= 0)
                return;

            _logger.LogInboundTrace(
                IntegrationLogEvents.RetryDelayed,
                context.Envelope,
                () => new object?[]
                {
                    delay
                });

            await Task.Delay(delay).ConfigureAwait(false);
        }
    }
}
