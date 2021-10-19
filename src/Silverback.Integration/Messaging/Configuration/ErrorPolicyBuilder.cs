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
    /// <param name="policyConfigurationAction">
    ///     The optional additional configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="ErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    public ErrorPolicyChainBuilder Stop(Action<StopConsumerErrorPolicy>? policyConfigurationAction = null) =>
        _chainBuilder.ThenStop(policyConfigurationAction);

    /// <summary>
    ///     Adds a <see cref="SkipMessageErrorPolicy" /> that skips the messages that fail to be processed.
    /// </summary>
    /// <param name="policyConfigurationAction">
    ///     The optional additional configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="ErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    public ErrorPolicyChainBuilder Skip(Action<SkipMessageErrorPolicy>? policyConfigurationAction = null) =>
        _chainBuilder.ThenSkip(policyConfigurationAction);

    /// <summary>
    ///     Adds a <see cref="RetryErrorPolicy" /> that retries to process the messages that previously failed to be to processed.
    /// </summary>
    /// <param name="policyConfigurationAction">
    ///     The optional additional configuration.
    /// </param>
    /// <returns>
    ///     The <see cref="ErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    public ErrorPolicyChainBuilder Retry(Action<RetryErrorPolicy> policyConfigurationAction) =>
        Retry(null, null, null, policyConfigurationAction);

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
    public ErrorPolicyChainBuilder Retry(int retriesCount, Action<RetryErrorPolicy> policyConfigurationAction)
        => Retry(retriesCount, null, null, policyConfigurationAction);

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
    public ErrorPolicyChainBuilder Retry(int retriesCount, TimeSpan initialDelay, Action<RetryErrorPolicy> policyConfigurationAction)
        => Retry(retriesCount, initialDelay, null, policyConfigurationAction);

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
    public ErrorPolicyChainBuilder Retry(
        int? retriesCount = null,
        TimeSpan? initialDelay = null,
        TimeSpan? delayIncrement = null,
        Action<RetryErrorPolicy>? policyConfigurationAction = null) =>
        _chainBuilder.ThenRetry(retriesCount, initialDelay, delayIncrement, policyConfigurationAction);

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
    public ErrorPolicyChainBuilder Move(
        ProducerConfiguration producerConfiguration,
        Action<MoveMessageErrorPolicy>? policyConfigurationAction = null) =>
        _chainBuilder.ThenMove(producerConfiguration, policyConfigurationAction);

    /// <summary>
    ///     Builds the <see cref="IErrorPolicy" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="IErrorPolicy" />.
    /// </returns>
    public IErrorPolicy Build() => _chainBuilder.Build();
}
