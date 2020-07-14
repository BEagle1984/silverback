// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Diagnostics;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;
using Silverback.Util;

namespace Silverback.Messaging.ErrorHandling
{
    /// <inheritdoc cref="IErrorPolicy" />
    public abstract class ErrorPolicyBase : IErrorPolicy
    {
        private readonly IServiceProvider _serviceProvider;

        private readonly ILogger<ErrorPolicyBase> _logger;

        private readonly List<Type> _excludedExceptions = new List<Type>();

        private readonly List<Type> _includedExceptions = new List<Type>();

        private Func<IRawInboundEnvelope, Exception, bool>? _applyRule;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ErrorPolicyBase" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" />.
        /// </param>
        /// <param name="logger">
        ///     The <see cref="ILogger" />.
        /// </param>
        protected ErrorPolicyBase(
            IServiceProvider serviceProvider,
            ILogger<ErrorPolicyBase> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        internal Func<IReadOnlyCollection<IRawInboundEnvelope>, object>? MessageToPublishFactory { get; private set; }

        internal int MaxFailedAttemptsSetting { get; private set; } = -1;

        /// <summary>
        ///     Restricts the application of this policy to the specified exception type only. It is possible to
        ///     combine multiple calls to <c>ApplyTo</c> and <c>Exclude</c>.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the exception to be handled.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="ErrorPolicyBase" /> so that additional calls can be chained.
        /// </returns>
        public ErrorPolicyBase ApplyTo<T>()
            where T : Exception
        {
            ApplyTo(typeof(T));
            return this;
        }

        /// <summary>
        ///     Restricts the application of this policy to the specified exception type only. It is possible to
        ///     combine multiple calls to <c>ApplyTo</c> and <c>Exclude</c>.
        /// </summary>
        /// <param name="exceptionType">
        ///     The type of the exception to be handled.
        /// </param>
        /// <returns>
        ///     The <see cref="ErrorPolicyBase" /> so that additional calls can be chained.
        /// </returns>
        public ErrorPolicyBase ApplyTo(Type exceptionType)
        {
            _includedExceptions.Add(exceptionType);
            return this;
        }

        /// <summary>
        ///     Restricts the application of this policy to all exceptions but the specified type. It is possible to
        ///     combine multiple calls to <c>ApplyTo</c> and <c>Exclude</c>.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the exception to be ignored.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="ErrorPolicyBase" /> so that additional calls can be chained.
        /// </returns>
        public ErrorPolicyBase Exclude<T>()
            where T : Exception
        {
            Exclude(typeof(T));
            return this;
        }

        /// <summary>
        ///     Restricts the application of this policy to all exceptions but the specified type. It is possible to
        ///     combine multiple calls to <c>ApplyTo</c> and <c>Exclude</c>.
        /// </summary>
        /// <param name="exceptionType">
        ///     The type of the exception to be ignored.
        /// </param>
        /// <returns>
        ///     The <see cref="ErrorPolicyBase" /> so that additional calls can be chained.
        /// </returns>
        public ErrorPolicyBase Exclude(Type exceptionType)
        {
            _excludedExceptions.Add(exceptionType);
            return this;
        }

        /// <summary>
        ///     Specifies a predicate to be used to determine whether the policy has to be applied according to the
        ///     current message and exception.
        /// </summary>
        /// <param name="applyRule">
        ///     The predicate.
        /// </param>
        /// <returns>
        ///     The <see cref="ErrorPolicyBase" /> so that additional calls can be chained.
        /// </returns>
        public ErrorPolicyBase ApplyWhen(Func<IRawInboundEnvelope, Exception, bool> applyRule)
        {
            _applyRule = applyRule;
            return this;
        }

        /// <summary>
        ///     Specifies how many times this rule can be applied to the same message. Most useful for
        ///     <see cref="RetryErrorPolicy" /> and <see cref="MoveMessageErrorPolicy" /> to limit the number of
        ///     iterations. If multiple policies are chained in an <see cref="ErrorPolicyChain" /> then the next
        ///     policy will be triggered after the allotted amount of retries.
        /// </summary>
        /// <param name="maxFailedAttempts">
        ///     The number of retries.
        /// </param>
        /// <returns>
        ///     The <see cref="ErrorPolicyBase" /> so that additional calls can be chained.
        /// </returns>
        public ErrorPolicyBase MaxFailedAttempts(int maxFailedAttempts)
        {
            MaxFailedAttemptsSetting = maxFailedAttempts;
            return this;
        }

        /// <summary>
        ///     Specify a delegate to create a message to be published to the internal bus when this policy is
        ///     applied. Useful to execute some custom code.
        /// </summary>
        /// <param name="factory">
        ///     The factory returning the message to be published.
        /// </param>
        /// <returns>
        ///     The <see cref="ErrorPolicyBase" /> so that additional calls can be chained.
        /// </returns>
        public ErrorPolicyBase Publish(Func<IReadOnlyCollection<IRawInboundEnvelope>, object> factory)
        {
            MessageToPublishFactory = factory;
            return this;
        }

        /// <inheritdoc cref="IErrorPolicy.CanHandle" />
        public virtual bool CanHandle(IReadOnlyCollection<IRawInboundEnvelope> envelopes, Exception exception) =>
            envelopes.All(envelope => CanHandle(envelope, exception));

        /// <summary>
        ///     Returns a boolean value indicating whether the policy can handle the specified envelope and the
        ///     specified exception.
        /// </summary>
        /// <param name="envelope">
        ///     The envelope that failed to be processed.
        /// </param>
        /// <param name="exception">
        ///     The exception that was thrown during the processing.
        /// </param>
        /// <returns>
        ///     A value indicating whether the specified envelopes and exception can be handled.
        /// </returns>
        /// <remarks>
        ///     In the default implementation, this method is called for each envelope passed to the overload
        ///     accepting a collection of envelopes.
        /// </remarks>
        public virtual bool CanHandle(IRawInboundEnvelope envelope, Exception exception)
        {
            Check.NotNull(envelope, nameof(envelope));
            Check.NotNull(exception, nameof(exception));

            var failedAttempts = envelope.Headers.GetValueOrDefault<int>(DefaultMessageHeaders.FailedAttempts);

            if (MaxFailedAttemptsSetting >= 0 && failedAttempts > MaxFailedAttemptsSetting)
            {
                var traceString = $"The policy '{GetType().Name}' will be skipped because the current failed " +
                                  $"attempts ({failedAttempts}) exceeds the configured maximum attempts " +
                                  $"({MaxFailedAttemptsSetting}).";

                _logger.LogTraceWithMessageInfo(EventIds.ErrorPolicyBaseSkipPolicyBecauseOfFailedAttempts, traceString, envelope);

                return false;
            }

            if (_includedExceptions.Any() && _includedExceptions.All(e => !e.IsInstanceOfType(exception)))
            {
                var traceString = $"The policy '{GetType().Name}' will be skipped because the " +
                                  $"{exception.GetType().Name} is not in the list of handled exceptions.";

                _logger.LogTraceWithMessageInfo(
                    EventIds.ErrorPolicyBaseSkipPolicyBecauseExceptionIsNotInlcuded,
                    traceString,
                    envelope);

                return false;
            }

            if (_excludedExceptions.Any(e => e.IsInstanceOfType(exception)))
            {
                var traceString = $"The policy '{GetType().Name}' will be skipped because the " +
                                  $"{exception.GetType().Name} is in the list of excluded exceptions.";

                _logger.LogTraceWithMessageInfo(EventIds.ErrorPolicyBaseSkipPolicyBecauseExceptionIsExcluded, traceString, envelope);

                return false;
            }

            if (_applyRule != null && !_applyRule.Invoke(envelope, exception))
            {
                var traceString = $"The policy '{GetType().Name}' will be skipped because the apply rule has been " +
                                  "evaluated and returned false.";

                _logger.LogTraceWithMessageInfo(EventIds.ErrorPolicyBaseSkipPolicyBecauseOfApplyRule, traceString, envelope);

                return false;
            }

            return true;
        }

        /// <inheritdoc cref="IErrorPolicy.HandleError" />
        public async Task<ErrorAction> HandleError(
            IReadOnlyCollection<IRawInboundEnvelope> envelopes,
            Exception exception)
        {
            ErrorAction result = await ApplyPolicy(envelopes, exception).ConfigureAwait(false);

            if (MessageToPublishFactory != null)
            {
                using var scope = _serviceProvider.CreateScope();
                await scope.ServiceProvider.GetRequiredService<IPublisher>()
                    .PublishAsync(MessageToPublishFactory.Invoke(envelopes))
                    .ConfigureAwait(false);
            }

            return result;
        }

        /// <summary>
        ///     Executes the current policy and returns the <see cref="ErrorAction" /> to be performed by the
        ///     consumer.
        /// </summary>
        /// <param name="envelopes">
        ///     The envelopes that failed to be processed.
        /// </param>
        /// <param name="exception">
        ///     The exception that was thrown during the processing.
        /// </param>
        /// <returns>
        ///     A <see cref="Task" /> representing the asynchronous operation. The task result contains the action
        ///     that the consumer should perform (e.g. skip the message or stop consuming).
        /// </returns>
        protected abstract Task<ErrorAction> ApplyPolicy(
            IReadOnlyCollection<IRawInboundEnvelope> envelopes,
            Exception exception);
    }
}
