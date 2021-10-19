// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Inbound.ErrorHandling;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="IErrorPolicy" />.
/// </summary>
public class ErrorPolicyChainBuilder
{
    private readonly IList<ErrorPolicyBase> _errorPolicies = new List<ErrorPolicyBase>();

    /// <summary>
    ///     Initializes a new instance of the <see cref="ErrorPolicyChainBuilder" /> class.
    /// </summary>
    /// <param name="endpointsConfigurationBuilder">
    ///     The optional the <see cref="EndpointsConfigurationBuilder" /> that instantiated the builder.
    /// </param>
    public ErrorPolicyChainBuilder(EndpointsConfigurationBuilder? endpointsConfigurationBuilder = null)
    {
        EndpointsConfigurationBuilder = endpointsConfigurationBuilder;
    }

    /// <summary>
    ///     Gets the <see cref="EndpointsConfigurationBuilder" /> that instantiated the builder.
    /// </summary>
    public EndpointsConfigurationBuilder? EndpointsConfigurationBuilder { get; }

    /// <summary>
    ///     Adds a <see cref="StopConsumerErrorPolicy" /> that stops the consumer when an exception is thrown during the message processing.
    /// </summary>
    /// <param name="policyConfigurationAction">
    ///     The optional additional configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="ErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    public ErrorPolicyChainBuilder ThenStop(Action<StopConsumerErrorPolicy>? policyConfigurationAction = null)
    {
        StopConsumerErrorPolicy policy = new();
        policyConfigurationAction?.Invoke(policy);
        _errorPolicies.Add(policy);
        return this;
    }

    /// <summary>
    ///     Adds a <see cref="SkipMessageErrorPolicy" /> that skips the messages that fail to be processed.
    /// </summary>
    /// <param name="policyConfigurationAction">
    ///     The optional additional configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="ErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    public ErrorPolicyChainBuilder ThenSkip(Action<SkipMessageErrorPolicy>? policyConfigurationAction = null)
    {
        SkipMessageErrorPolicy policy = new();
        policyConfigurationAction?.Invoke(policy);
        _errorPolicies.Add(policy);
        return this;
    }

    /// <summary>
    ///     Adds a <see cref="RetryErrorPolicy" /> that retries to process the messages that previously failed to be to processed.
    /// </summary>
    /// <param name="policyConfigurationAction">
    ///     The optional additional configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="ErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    public ErrorPolicyChainBuilder ThenRetry(Action<RetryErrorPolicy> policyConfigurationAction) =>
        ThenRetry(null, null, null, policyConfigurationAction);

    /// <summary>
    ///     Adds a <see cref="RetryErrorPolicy" /> that retries to process the messages that previously failed to be to processed.
    /// </summary>
    /// <param name="retriesCount">
    ///     The maximum number of retries to be performed.
    /// </param>
    /// <param name="policyConfigurationAction">
    ///     The optional additional configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="ErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    public ErrorPolicyChainBuilder ThenRetry(int retriesCount, Action<RetryErrorPolicy> policyConfigurationAction)
        => ThenRetry(retriesCount, null, null, policyConfigurationAction);

    /// <summary>
    ///     Adds a <see cref="RetryErrorPolicy" /> that retries to process the messages that previously failed to be to processed.
    /// </summary>
    /// <param name="retriesCount">
    ///     The maximum number of retries to be performed.
    /// </param>
    /// <param name="initialDelay">
    ///     The optional delay to be applied to the first retry.
    /// </param>
    /// <param name="policyConfigurationAction">
    ///     The optional additional configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="ErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    public ErrorPolicyChainBuilder ThenRetry(
        int retriesCount,
        TimeSpan initialDelay,
        Action<RetryErrorPolicy> policyConfigurationAction)
        => ThenRetry(retriesCount, initialDelay, null, policyConfigurationAction);

    /// <summary>
    ///     Adds a <see cref="RetryErrorPolicy" /> that retries to process the messages that previously failed to be to processed.
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
    ///     The optional additional configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="ErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    public ErrorPolicyChainBuilder ThenRetry(
        int? retriesCount = null,
        TimeSpan? initialDelay = null,
        TimeSpan? delayIncrement = null,
        Action<RetryErrorPolicy>? policyConfigurationAction = null)
    {
        RetryErrorPolicy policy = new(initialDelay, delayIncrement);

        if (retriesCount.HasValue)
            policy.MaxFailedAttempts(retriesCount);

        policyConfigurationAction?.Invoke(policy);

        _errorPolicies.Add(policy);
        return this;
    }

    /// <summary>
    ///     Adds a <see cref="MoveMessageErrorPolicy" /> that moves the messages that fail to be processed to the configured endpoint.
    /// </summary>
    /// <param name="producerConfiguration">
    ///     The configuration of the producer to be used to move the message.
    /// </param>
    /// <param name="policyConfigurationAction">
    ///     The optional additional configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="ErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    public ErrorPolicyChainBuilder ThenMove(
        ProducerConfiguration producerConfiguration,
        Action<MoveMessageErrorPolicy>? policyConfigurationAction = null)
    {
        MoveMessageErrorPolicy policy = new(producerConfiguration);
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
