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
    /// <param name="policyBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="StopConsumerErrorPolicyBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="ErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    public ErrorPolicyChainBuilder ThenStop(Action<StopConsumerErrorPolicyBuilder>? policyBuilderAction = null)
    {
        StopConsumerErrorPolicyBuilder builder = new();
        policyBuilderAction?.Invoke(builder);
        _errorPolicies.Add(builder.Build());
        return this;
    }

    /// <summary>
    ///     Adds a <see cref="SkipMessageErrorPolicy" /> that skips the messages that fail to be processed.
    /// </summary>
    /// <param name="policyBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="SkipMessageErrorPolicyBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="ErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    public ErrorPolicyChainBuilder ThenSkip(Action<SkipMessageErrorPolicyBuilder>? policyBuilderAction = null)
    {
        SkipMessageErrorPolicyBuilder builder = new();
        policyBuilderAction?.Invoke(builder);
        _errorPolicies.Add(builder.Build());
        return this;
    }

    /// <summary>
    ///     Adds a <see cref="RetryErrorPolicy" /> that retries to process the messages that previously failed to be to processed.
    /// </summary>
    /// <param name="policyBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="RetryErrorPolicyBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="ErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    public ErrorPolicyChainBuilder ThenRetry(Action<RetryErrorPolicyBuilder>? policyBuilderAction = null)
    {
        RetryErrorPolicyBuilder builder = new();
        policyBuilderAction?.Invoke(builder);
        _errorPolicies.Add(builder.Build());
        return this;
    }

    /// <summary>
    ///     Adds a <see cref="RetryErrorPolicy" /> that retries to process the messages that previously failed to be to processed.
    /// </summary>
    /// <param name="retriesCount">
    ///     The maximum number of retries to be performed.
    /// </param>
    /// <param name="policyBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="RetryErrorPolicyBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="ErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    public ErrorPolicyChainBuilder ThenRetry(int retriesCount, Action<RetryErrorPolicyBuilder>? policyBuilderAction = null)
    {
        RetryErrorPolicyBuilder builder = new();
        builder.WithMaxRetries(retriesCount);
        policyBuilderAction?.Invoke(builder);
        _errorPolicies.Add(builder.Build());
        return this;
    }

    /// <summary>
    ///     Adds a <see cref="MoveMessageErrorPolicy" /> that moves the messages that fail to be processed to the configured endpoint.
    /// </summary>
    /// <param name="producerConfiguration">
    ///     The configuration of the producer to be used to move the message.
    /// </param>
    /// <param name="policyBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="MoveMessageErrorPolicyBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="ErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    public ErrorPolicyChainBuilder ThenMove(
        ProducerConfiguration producerConfiguration,
        Action<MoveMessageErrorPolicyBuilder>? policyBuilderAction = null)
    {
        MoveMessageErrorPolicyBuilder builder = new(producerConfiguration);
        policyBuilderAction?.Invoke(builder);
        _errorPolicies.Add(builder.Build());
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
            throw new SilverbackConfigurationException("At least 1 error policy is required.");

        if (_errorPolicies.Count == 1)
            return _errorPolicies[0];

        return new ErrorPolicyChain(_errorPolicies);
    }
}
