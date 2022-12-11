// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Diagnostics;
using Silverback.Messaging.Broker.Behaviors;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.Consuming.ErrorHandling;

/// <inheritdoc cref="IErrorPolicy" />
public abstract class ErrorPolicyImplementation : IErrorPolicyImplementation
{
    private readonly int? _maxFailedAttempts;

    private readonly IReadOnlyCollection<Type> _excludedExceptions;

    private readonly IReadOnlyCollection<Type> _includedExceptions;

    private readonly Func<IRawInboundEnvelope, Exception, bool>? _applyRule;

    private readonly Func<IRawInboundEnvelope, Exception, object?>? _messageToPublishFactory;

    private readonly IServiceProvider _serviceProvider;

    private readonly IConsumerLogger<ErrorPolicyBase> _logger;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ErrorPolicyImplementation" /> class.
    /// </summary>
    /// <param name="maxFailedAttempts">
    ///     The number of times this policy should be applied to the same message in case of multiple failed
    ///     attempts.
    /// </param>
    /// <param name="excludedExceptions">
    ///     The collection of exception types this policy doesn't have to be applied to.
    /// </param>
    /// <param name="includedExceptions">
    ///     The collection of exception types this policy have to be applied to.
    /// </param>
    /// <param name="applyRule">
    ///     The custom apply rule function.
    /// </param>
    /// <param name="messageToPublishFactory">
    ///     The factory that builds the message to be published after the policy
    ///     is applied.
    /// </param>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" />.
    /// </param>
    /// <param name="logger">
    ///     The <see cref="IConsumerLogger{TCategoryName}" />.
    /// </param>
    protected ErrorPolicyImplementation(
        int? maxFailedAttempts,
        IReadOnlyCollection<Type> excludedExceptions,
        IReadOnlyCollection<Type> includedExceptions,
        Func<IRawInboundEnvelope, Exception, bool>? applyRule,
        Func<IRawInboundEnvelope, Exception, object?>? messageToPublishFactory,
        IServiceProvider serviceProvider,
        IConsumerLogger<ErrorPolicyBase> logger)
    {
        _maxFailedAttempts = maxFailedAttempts;
        _excludedExceptions = Check.NotNull(excludedExceptions, nameof(excludedExceptions));
        _includedExceptions = Check.NotNull(includedExceptions, nameof(includedExceptions));
        _applyRule = applyRule;
        _messageToPublishFactory = messageToPublishFactory;

        _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        _logger = Check.NotNull(logger, nameof(logger));
    }

    /// <inheritdoc cref="IErrorPolicyImplementation.CanHandle" />
    public virtual bool CanHandle(ConsumerPipelineContext context, Exception exception)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(exception, nameof(exception));

        int failedAttempts =
            context.Envelope.Headers.GetValueOrDefault<int>(DefaultMessageHeaders.FailedAttempts);

        if (_maxFailedAttempts != null && failedAttempts > _maxFailedAttempts)
        {
            _logger.LogConsumerTrace(
                IntegrationLogEvents.PolicyMaxFailedAttemptsExceeded,
                context.Envelope,
                () => new object?[]
                {
                    GetType().Name,
                    failedAttempts,
                    _maxFailedAttempts
                });

            return false;
        }

        if (_includedExceptions.Any() &&
            _includedExceptions.All(type => !type.IsInstanceOfType(exception)))
        {
            _logger.LogConsumerTrace(
                IntegrationLogEvents.PolicyExceptionNotIncluded,
                context.Envelope,
                () => new object?[]
                {
                    GetType().Name,
                    exception.GetType().Name
                });

            return false;
        }

        if (_excludedExceptions.Any(type => type.IsInstanceOfType(exception)))
        {
            _logger.LogConsumerTrace(
                IntegrationLogEvents.PolicyExceptionExcluded,
                context.Envelope,
                () => new object?[]
                {
                    GetType().Name,
                    exception.GetType().Name
                });

            return false;
        }

        if (_applyRule != null && !_applyRule.Invoke(context.Envelope, exception))
        {
            _logger.LogConsumerTrace(
                IntegrationLogEvents.PolicyApplyRuleReturnedFalse,
                context.Envelope,
                () => new object?[]
                {
                    GetType().Name
                });

            return false;
        }

        return true;
    }

    /// <inheritdoc cref="IErrorPolicyImplementation.HandleErrorAsync" />
    public async Task<bool> HandleErrorAsync(ConsumerPipelineContext context, Exception exception)
    {
        Check.NotNull(context, nameof(context));
        Check.NotNull(exception, nameof(exception));

        bool result = await ApplyPolicyAsync(context, exception).ConfigureAwait(false);

        if (_messageToPublishFactory == null)
            return result;

        object? message = _messageToPublishFactory.Invoke(context.Envelope, exception);
        if (message == null)
            return result;

        using IServiceScope scope = _serviceProvider.CreateScope();
        await scope.ServiceProvider.GetRequiredService<IPublisher>()
            .PublishAsync(message)
            .ConfigureAwait(false);

        return result;
    }

    /// <summary>
    ///     Executes the current policy.
    /// </summary>
    /// <param name="context">
    ///     The <see cref="ConsumerPipelineContext" /> related to the message that failed to be processed.
    /// </param>
    /// <param name="exception">
    ///     The exception that was thrown during the processing.
    /// </param>
    /// <returns>
    ///     A <see cref="Task{TResult}" /> representing the asynchronous operation. The task result contains the
    ///     action that the consumer should perform (e.g. skip the message or stop consuming).
    /// </returns>
    protected abstract Task<bool> ApplyPolicyAsync(ConsumerPipelineContext context, Exception exception);
}
