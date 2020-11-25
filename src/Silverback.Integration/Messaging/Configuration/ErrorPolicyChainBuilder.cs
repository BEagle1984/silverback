// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Builds the <see cref="IErrorPolicy" />.
    /// </summary>
    public class ErrorPolicyChainBuilder : IErrorPolicyChainBuilder
    {
        private readonly IList<ErrorPolicyBase> _errorPolicies = new List<ErrorPolicyBase>();

        /// <summary>
        ///     Initializes a new instance of the <see cref="ErrorPolicyChainBuilder" /> class.
        /// </summary>
        internal ErrorPolicyChainBuilder()
        {
        }

        /// <inheritdoc cref="IErrorPolicyChainBuilder.ThenStop"/>>
        public IErrorPolicyChainBuilder ThenStop(Action<StopConsumerErrorPolicy>? policyConfigurationAction = null)
        {
            var policy = new StopConsumerErrorPolicy();
            policyConfigurationAction?.Invoke(policy);
            _errorPolicies.Add(policy);
            return this;
        }

        /// <inheritdoc cref="IErrorPolicyChainBuilder.ThenSkip"/>>
        public IErrorPolicyChainBuilder ThenSkip(Action<SkipMessageErrorPolicy>? policyConfigurationAction = null)
        {
            var policy = new SkipMessageErrorPolicy();
            policyConfigurationAction?.Invoke(policy);
            _errorPolicies.Add(policy);
            return this;
        }

        /// <inheritdoc cref="IErrorPolicyChainBuilder.ThenRetry(Action{RetryErrorPolicy})"/>>
        public IErrorPolicyChainBuilder ThenRetry(Action<RetryErrorPolicy> policyConfigurationAction) =>
            ThenRetry(null, null, null, policyConfigurationAction);

        /// <inheritdoc cref="IErrorPolicyChainBuilder.ThenRetry(int, Action{RetryErrorPolicy})"/>>
        public IErrorPolicyChainBuilder ThenRetry(
            int retriesCount,
            Action<RetryErrorPolicy> policyConfigurationAction)
            => ThenRetry(retriesCount, null, null, policyConfigurationAction);

        /// <inheritdoc cref="IErrorPolicyChainBuilder.ThenRetry(int, TimeSpan, Action{RetryErrorPolicy})"/>>
        public IErrorPolicyChainBuilder ThenRetry(
            int retriesCount,
            TimeSpan initialDelay,
            Action<RetryErrorPolicy> policyConfigurationAction)
            => ThenRetry(retriesCount, initialDelay, null, policyConfigurationAction);

        /// <inheritdoc cref="IErrorPolicyChainBuilder.ThenRetry(int?, TimeSpan?, TimeSpan?, Action{RetryErrorPolicy}?)"/>>
        public IErrorPolicyChainBuilder ThenRetry(
            int? retriesCount = null,
            TimeSpan? initialDelay = null,
            TimeSpan? delayIncrement = null,
            Action<RetryErrorPolicy>? policyConfigurationAction = null)
        {
            var policy = new RetryErrorPolicy(initialDelay, delayIncrement);

            if (retriesCount.HasValue)
                policy.MaxFailedAttempts(retriesCount);

            policyConfigurationAction?.Invoke(policy);

            _errorPolicies.Add(policy);
            return this;
        }

        /// <inheritdoc cref="IErrorPolicyChainBuilder.ThenMove(IProducerEndpoint, Action{MoveMessageErrorPolicy}?)"/>>
        public IErrorPolicyChainBuilder ThenMove(
            IProducerEndpoint endpoint,
            Action<MoveMessageErrorPolicy>? policyConfigurationAction = null)
        {
            var policy = new MoveMessageErrorPolicy(endpoint);
            policyConfigurationAction?.Invoke(policy);
            _errorPolicies.Add(policy);
            return this;
        }

        /// <summary>
        ///     Builds the <see cref="IErrorPolicy" /> instance.
        /// </summary>
        /// <returns>
        ///     The <see cref="IMessageSerializer" />.
        /// </returns>
        public IErrorPolicy Build()
        {
            if (_errorPolicies.Count == 0)
                throw new InvalidOperationException("No policy was specified.");

            if (_errorPolicies.Count == 1)
                return _errorPolicies[0];

            return new ErrorPolicyChain(_errorPolicies);
        }
    }
}
