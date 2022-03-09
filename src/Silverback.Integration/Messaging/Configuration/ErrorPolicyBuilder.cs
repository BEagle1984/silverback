// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Inbound.ErrorHandling;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="IErrorPolicy" />.
/// </summary>
public class ErrorPolicyBuilder
{
    private readonly ErrorPolicyChainBuilder _chainBuilder;

    /// <summary>
    ///     Initializes a new instance of the <see cref="ErrorPolicyBuilder" /> class.
    /// </summary>
    /// <param name="endpointsConfigurationBuilder">
    ///     The optional <see cref="EndpointsConfigurationBuilder" /> that instantiated the builder.
    /// </param>
    public ErrorPolicyBuilder(EndpointsConfigurationBuilder? endpointsConfigurationBuilder = null)
    {
        EndpointsConfigurationBuilder = endpointsConfigurationBuilder;
        _chainBuilder = new ErrorPolicyChainBuilder(endpointsConfigurationBuilder);
    }

    /// <summary>
    ///     Gets the <see cref="EndpointsConfigurationBuilder" /> that instantiated the builder.
    /// </summary>
    internal EndpointsConfigurationBuilder? EndpointsConfigurationBuilder { get; }

    /// <summary>
    ///     Adds a <see cref="StopConsumerErrorPolicy" /> that stops the consumer when an exception is thrown during the message processing.
    /// </summary>
    /// <param name="policyBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="StopConsumerErrorPolicyBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="ErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    public ErrorPolicyChainBuilder Stop(Action<StopConsumerErrorPolicyBuilder>? policyBuilderAction = null) =>
        _chainBuilder.ThenStop(policyBuilderAction);

    /// <summary>
    ///     Adds a <see cref="SkipMessageErrorPolicy" /> that skips the messages that fail to be processed.
    /// </summary>
    /// <param name="policyBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="SkipMessageErrorPolicyBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="ErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    public ErrorPolicyChainBuilder Skip(Action<SkipMessageErrorPolicyBuilder>? policyBuilderAction = null) =>
        _chainBuilder.ThenSkip(policyBuilderAction);

    /// <summary>
    ///     Adds a <see cref="RetryErrorPolicy" /> that retries to process the messages that previously failed to be to processed.
    /// </summary>
    /// <param name="policyBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="RetryErrorPolicyBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="ErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    public ErrorPolicyChainBuilder Retry(Action<RetryErrorPolicyBuilder>? policyBuilderAction = null) =>
        _chainBuilder.ThenRetry(policyBuilderAction);

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
    public ErrorPolicyChainBuilder Retry(int retriesCount, Action<RetryErrorPolicyBuilder>? policyBuilderAction = null) =>
        _chainBuilder.ThenRetry(retriesCount, policyBuilderAction);

    /// <summary>
    ///     Adds a <see cref="MoveMessageErrorPolicy" /> that moves the messages that fail to be processed to the configured endpoint.
    /// </summary>
    /// <param name="producerConfiguration">
    ///     The configuration of the producer to be used to move the message.
    /// </param>
    /// <param name="policyBuilderAction">
    ///     The optional additional configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="ErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    public ErrorPolicyChainBuilder Move(
        ProducerConfiguration producerConfiguration,
        Action<MoveMessageErrorPolicyBuilder>? policyBuilderAction = null) =>
        _chainBuilder.ThenMove(producerConfiguration, policyBuilderAction);

    /// <summary>
    ///     Builds the <see cref="IErrorPolicy" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="IErrorPolicy" />.
    /// </returns>
    public IErrorPolicy Build() => _chainBuilder.Build();
}
