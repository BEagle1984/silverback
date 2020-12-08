// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Builds the <see cref="IErrorPolicy" />.
    /// </summary>
    public class ErrorPolicyBuilder : IErrorPolicyBuilder
    {
        private readonly ErrorPolicyChainBuilder _chainBuilder = new();

        /// <summary>
        ///     Initializes a new instance of the <see cref="ErrorPolicyBuilder" /> class.
        /// </summary>
        /// <param name="endpointsConfigurationBuilder">
        ///     The optional reference to the <see cref="IEndpointsConfigurationBuilder" /> that instantiated the
        ///     builder.
        /// </param>
        public ErrorPolicyBuilder(IEndpointsConfigurationBuilder? endpointsConfigurationBuilder = null)
        {
            EndpointsConfigurationBuilder = endpointsConfigurationBuilder;
        }

        /// <summary>
        ///     Gets the <see cref="IEndpointsConfigurationBuilder" /> that instantiated the builder.
        /// </summary>
        public IEndpointsConfigurationBuilder? EndpointsConfigurationBuilder { get; }

        /// <inheritdoc cref="IErrorPolicyBuilder.Stop" />
        public IErrorPolicyChainBuilder Stop(Action<StopConsumerErrorPolicy>? policyConfigurationAction = null) =>
            _chainBuilder.ThenStop(policyConfigurationAction);

        /// <inheritdoc cref="IErrorPolicyBuilder.Skip" />
        public IErrorPolicyChainBuilder Skip(Action<SkipMessageErrorPolicy>? policyConfigurationAction = null) =>
            _chainBuilder.ThenSkip(policyConfigurationAction);

        /// <inheritdoc cref="IErrorPolicyBuilder.Retry(Action{RetryErrorPolicy})" />
        public IErrorPolicyChainBuilder Retry(Action<RetryErrorPolicy> policyConfigurationAction) =>
            Retry(null, null, null, policyConfigurationAction);

        /// <inheritdoc cref="IErrorPolicyBuilder.Retry(int, Action{RetryErrorPolicy})" />
        public IErrorPolicyChainBuilder Retry(
            int retriesCount,
            Action<RetryErrorPolicy> policyConfigurationAction)
            => Retry(retriesCount, null, null, policyConfigurationAction);

        /// <inheritdoc cref="IErrorPolicyBuilder.Retry(int, TimeSpan, Action{RetryErrorPolicy})" />
        public IErrorPolicyChainBuilder Retry(
            int retriesCount,
            TimeSpan initialDelay,
            Action<RetryErrorPolicy> policyConfigurationAction)
            => Retry(retriesCount, initialDelay, null, policyConfigurationAction);

        /// <inheritdoc cref="IErrorPolicyBuilder.Retry(int?, TimeSpan?, TimeSpan?, Action{RetryErrorPolicy}?)" />
        public IErrorPolicyChainBuilder Retry(
            int? retriesCount = null,
            TimeSpan? initialDelay = null,
            TimeSpan? delayIncrement = null,
            Action<RetryErrorPolicy>? policyConfigurationAction = null) =>
            _chainBuilder.ThenRetry(retriesCount, initialDelay, delayIncrement, policyConfigurationAction);

        /// <inheritdoc cref="IErrorPolicyBuilder.Move(IProducerEndpoint, Action{MoveMessageErrorPolicy})" />
        public IErrorPolicyChainBuilder Move(
            IProducerEndpoint endpoint,
            Action<MoveMessageErrorPolicy>? policyConfigurationAction = null) =>
            _chainBuilder.ThenMove(endpoint, policyConfigurationAction);

        /// <summary>
        ///     Builds the <see cref="IErrorPolicy" /> instance.
        /// </summary>
        /// <returns>
        ///     The <see cref="IMessageSerializer" />.
        /// </returns>
        public IErrorPolicy Build() => _chainBuilder.Build();
    }
}
