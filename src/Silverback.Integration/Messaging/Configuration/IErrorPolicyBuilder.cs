// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Inbound.ErrorHandling;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     Builds the <see cref="IErrorPolicy" />.
    /// </summary>
    public interface IErrorPolicyBuilder
    {
        /// <summary>
        ///     Adds a <see cref="StopConsumerErrorPolicy" /> that stops the consumer when an exception is thrown
        ///     during the message processing.
        /// </summary>
        /// <param name="policyConfigurationAction">
        ///     The (optional) additional configuration.
        /// </param>
        /// <returns>
        ///     The <see cref="IErrorPolicyChainBuilder" /> so that additional calls can be chained.
        /// </returns>
        IErrorPolicyChainBuilder Stop(Action<StopConsumerErrorPolicy>? policyConfigurationAction = null);

        /// <summary>
        ///     Adds a <see cref="SkipMessageErrorPolicy" /> that skips the messages that fail to be processed.
        /// </summary>
        /// <param name="policyConfigurationAction">
        ///     The (optional) additional configuration.
        /// </param>
        /// <returns>
        ///     The <see cref="IErrorPolicyChainBuilder" /> so that additional calls can be chained.
        /// </returns>
        IErrorPolicyChainBuilder Skip(Action<SkipMessageErrorPolicy>? policyConfigurationAction = null);

        /// <summary>
        ///     Adds a <see cref="RetryErrorPolicy" /> that retries to process the messages that previously failed to
        ///     be to processed.
        /// </summary>
        /// <param name="policyConfigurationAction">
        ///     The (optional) additional configuration.
        /// </param>
        /// <returns>
        ///     The <see cref="IErrorPolicyChainBuilder" /> so that additional calls can be chained.
        /// </returns>
        IErrorPolicyChainBuilder Retry(Action<RetryErrorPolicy> policyConfigurationAction);

        /// <summary>
        ///     Adds a <see cref="RetryErrorPolicy" /> that retries to process the messages that previously failed to
        ///     be to processed.
        /// </summary>
        /// <param name="retriesCount">
        ///     The maximum number of retries to be performed.
        /// </param>
        /// <param name="policyConfigurationAction">
        ///     The (optional) additional configuration.
        /// </param>
        /// <returns>
        ///     The <see cref="IErrorPolicyChainBuilder" /> so that additional calls can be chained.
        /// </returns>
        IErrorPolicyChainBuilder Retry(
            int retriesCount,
            Action<RetryErrorPolicy> policyConfigurationAction);

        /// <summary>
        ///     Adds a <see cref="RetryErrorPolicy" /> that retries to process the messages that previously failed to
        ///     be to processed.
        /// </summary>
        /// <param name="retriesCount">
        ///     The maximum number of retries to be performed.
        /// </param>
        /// <param name="initialDelay">
        ///     The optional delay to be applied to the first retry.
        /// </param>
        /// <param name="policyConfigurationAction">
        ///     The (optional) additional configuration.
        /// </param>
        /// <returns>
        ///     The <see cref="IErrorPolicyChainBuilder" /> so that additional calls can be chained.
        /// </returns>
        IErrorPolicyChainBuilder Retry(
            int retriesCount,
            TimeSpan initialDelay,
            Action<RetryErrorPolicy> policyConfigurationAction);

        /// <summary>
        ///     Adds a <see cref="RetryErrorPolicy" /> that retries to process the messages that previously failed to
        ///     be to processed.
        /// </summary>
        /// <param name="retriesCount">
        ///     The maximum number of retries to be performed.
        /// </param>
        /// <param name="initialDelay">
        ///     The optional delay to be applied to the first retry.
        /// </param>
        /// <param name="delayIncrement">
        ///     The optional increment to the delay to be applied at each retry.
        /// </param>
        /// <param name="policyConfigurationAction">
        ///     The (optional) additional configuration.
        /// </param>
        /// <returns>
        ///     The <see cref="IErrorPolicyChainBuilder" /> so that additional calls can be chained.
        /// </returns>
        IErrorPolicyChainBuilder Retry(
            int? retriesCount = null,
            TimeSpan? initialDelay = null,
            TimeSpan? delayIncrement = null,
            Action<RetryErrorPolicy>? policyConfigurationAction = null);

        /// <summary>
        ///     Adds a <see cref="MoveMessageErrorPolicy" /> that moves the messages that fail to  be processed to the
        ///     configured endpoint.
        /// </summary>
        /// <param name="endpoint">
        ///     The endpoint to move the message to.
        /// </param>
        /// <param name="policyConfigurationAction">
        ///     The (optional) additional configuration.
        /// </param>
        /// <returns>
        ///     The <see cref="IErrorPolicyChainBuilder" /> so that additional calls can be chained.
        /// </returns>
        IErrorPolicyChainBuilder Move(
            IProducerEndpoint endpoint,
            Action<MoveMessageErrorPolicy>? policyConfigurationAction = null);
    }
}
