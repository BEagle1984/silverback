// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Consuming.ErrorHandling;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="StopConsumerErrorPolicy" />.
/// </summary>
public class StopConsumerErrorPolicyBuilder : ErrorPolicyBaseBuilder<StopConsumerErrorPolicyBuilder>
{
    /// <inheritdoc cref="ErrorPolicyBaseBuilder{TBuilder}.This" />
    protected override StopConsumerErrorPolicyBuilder This => this;

    /// <inheritdoc cref="ErrorPolicyBaseBuilder{TBuilder}.BuildCore" />
    protected override ErrorPolicyBase BuildCore() => new StopConsumerErrorPolicy();
}
