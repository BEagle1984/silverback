// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Inbound.ErrorHandling;

/// <summary>
///     Builds an error policy that can be applied multiple times to the same message (e.g.
///     <see cref="RetryErrorPolicy" /> or <see cref="MoveMessageErrorPolicy" />).
/// </summary>
public abstract class RetryableErrorPolicyBase : ErrorPolicyBase
{
    /// <summary>
    ///     Specifies how many times this rule can be applied to the same message. If multiple policies are chained
    ///     in an <see cref="ErrorPolicyChain" /> then the next policy will be triggered after the allotted amount of
    ///     retries.
    /// </summary>
    /// <remarks>
    ///     <para>
    ///         The number of attempts at processing the message are stored locally, in memory and not persisted
    ///         anywhere. A restart or a Kafka rebalance would cause them to reset.
    ///     </para>
    ///     <para>
    ///         The messages are uniquely identified according to their <see cref="IBrokerMessageIdentifier" />, if
    ///         the message broker is providing a unique value like the Kafka offset. Otherwise the message id header
    ///         value is used (<see cref="DefaultMessageHeaders.MessageId" />). The mechanism will not work if no
    ///         unique identifier is provided (e.g. MQTT) and no id header is sent with the message.
    ///     </para>
    /// </remarks>
    /// <param name="maxFailedAttempts">
    ///     The number of retries.
    /// </param>
    /// <returns>
    ///     The <see cref="ErrorPolicyBase" /> so that additional calls can be chained.
    /// </returns>
    public ErrorPolicyBase MaxFailedAttempts(int? maxFailedAttempts)
    {
        if (maxFailedAttempts != null && maxFailedAttempts < 1)
        {
            throw new ArgumentOutOfRangeException(
                nameof(maxFailedAttempts),
                maxFailedAttempts,
                "MaxFailedAttempts must be greater or equal to 1.");
        }

        MaxFailedAttemptsCount = maxFailedAttempts;
        return this;
    }
}
