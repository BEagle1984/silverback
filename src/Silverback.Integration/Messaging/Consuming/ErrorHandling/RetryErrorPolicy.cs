// Copyright (c) 2024 Sergio Aquilini
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
///     This policy retries to process the message that previously failed to be to processed. An optional
///     delay can be specified.
/// </summary>
public record RetryErrorPolicy : ErrorPolicyBase
{
    internal RetryErrorPolicy()
    {
    }

    /// <summary>
    ///     Gets the delay to be applied to the first retry.
    /// </summary>
    public TimeSpan InitialDelay { get; init; } = TimeSpan.Zero;

    /// <summary>
    ///     Gets the increment to the delay to be applied at each retry.
    /// </summary>
    public TimeSpan DelayIncrement { get; init; } = TimeSpan.Zero;

    /// <summary>
    ///     Gets the factor to be applied to the delay to be applied at each retry.
    /// </summary>
    public double DelayFactor { get; init; } = 1.0;

    /// <summary>
    ///   Gets the maximum delay to be applied.
    /// </summary>
    public TimeSpan? MaxDelay { get; init; }

    /// <inheritdoc cref="ErrorPolicyBase.BuildCore" />
    protected override ErrorPolicyImplementation BuildCore(IServiceProvider serviceProvider) =>
        new RetryErrorPolicyImplementation(
            InitialDelay,
            DelayIncrement,
            DelayFactor,
            MaxDelay,
            MaxFailedAttempts,
            ExcludedExceptions,
            IncludedExceptions,
            ApplyRule,
            MessageToPublishFactory,
            serviceProvider,
            serviceProvider.GetRequiredService<IConsumerLogger<RetryErrorPolicy>>());

    private sealed class RetryErrorPolicyImplementation : ErrorPolicyImplementation
    {
        private readonly TimeSpan _initialDelay;

        private readonly TimeSpan _delayIncrement;

        private readonly double _delayFactor;

        private readonly TimeSpan? _maxDelay;

        private readonly IConsumerLogger<RetryErrorPolicy> _logger;

        public RetryErrorPolicyImplementation(
            TimeSpan initialDelay,
            TimeSpan delayIncrement,
            double delayFactor,
            TimeSpan? maxDelay,
            int? maxFailedAttempts,
            IReadOnlyCollection<Type> excludedExceptions,
            IReadOnlyCollection<Type> includedExceptions,
            Func<IRawInboundEnvelope, Exception, bool>? applyRule,
            Func<IRawInboundEnvelope, Exception, object?>? messageToPublishFactory,
            IServiceProvider serviceProvider,
            IConsumerLogger<RetryErrorPolicy> logger)
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
            _delayFactor = delayFactor;
            _maxDelay = maxDelay;
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

        [SuppressMessage("Design", "CA1031:Do not catch general exception types", Justification = "Exception logged")]
        private async Task<bool> TryRollbackAsync(ConsumerPipelineContext context, Exception exception)
        {
            try
            {
                await context.TransactionManager.RollbackAsync(exception, stopConsuming: false).ConfigureAwait(false);

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
            int failedAttempts = context.Envelope.Headers.GetValueOrDefault<int>(DefaultMessageHeaders.FailedAttempts);

            TimeSpan delay = IncrementalDelayHelper.Compute(failedAttempts, _initialDelay, _delayIncrement, _delayFactor, _maxDelay);

            if (delay <= TimeSpan.Zero)
                return;

            _logger.LogConsumerTrace(
                IntegrationLogEvents.RetryDelayed,
                context.Envelope,
                () =>
                [
                    delay
                ]);

            await Task.Delay(delay).ConfigureAwait(false);
        }
    }
}
