// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Serialization;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="IErrorPolicy" />.
/// </summary>
public interface IErrorPolicyChainBuilder
{
    /// <summary>
    ///     Adds a <see cref="StopConsumerErrorPolicy" /> that stops the consumer when an exception is thrown during the message processing.
    /// </summary>
    /// <param name="policyBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="StopConsumerErrorPolicyBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="IErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    IErrorPolicyChainBuilder ThenStop(Action<StopConsumerErrorPolicyBuilder>? policyBuilderAction = null);

    /// <summary>
    ///     Adds a <see cref="SkipMessageErrorPolicy" /> that skips the messages that fail to be processed.
    /// </summary>
    /// <param name="policyBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="SkipMessageErrorPolicyBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="IErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    IErrorPolicyChainBuilder ThenSkip(Action<SkipMessageErrorPolicyBuilder>? policyBuilderAction = null);

    /// <summary>
    ///     Adds a <see cref="RetryErrorPolicy" /> that retries to process the messages that previously failed to be to processed.
    /// </summary>
    /// <param name="policyBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="RetryErrorPolicyBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="IErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    IErrorPolicyChainBuilder ThenRetry(Action<RetryErrorPolicyBuilder>? policyBuilderAction = null);

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
    ///     The <see cref="IErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    IErrorPolicyChainBuilder ThenRetry(int retriesCount, Action<RetryErrorPolicyBuilder>? policyBuilderAction = null);

    /// <summary>
    ///     Adds a <see cref="MoveMessageErrorPolicy" /> that moves the messages that fail to be processed to the configured endpoint.
    /// </summary>
    /// <param name="endpointName">
    ///     The endpoint name. It could be either the topic/queue name or the friendly name of an endpoint that is already configured with
    ///     a producer.
    /// </param>
    /// <param name="policyBuilderAction">
    ///     An <see cref="Action{T}" /> that takes the <see cref="MoveMessageErrorPolicyBuilder" /> and configures it.
    /// </param>
    /// <returns>
    ///     The <see cref="IErrorPolicyChainBuilder" /> so that additional calls can be chained.
    /// </returns>
    IErrorPolicyChainBuilder ThenMoveTo(string endpointName, Action<MoveMessageErrorPolicyBuilder>? policyBuilderAction = null);

    /// <summary>
    ///     Builds the <see cref="IErrorPolicy" /> instance.
    /// </summary>
    /// <returns>
    ///     The <see cref="IMessageSerializer" />.
    /// </returns>
    IErrorPolicy Build();
}
