// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Consuming.ErrorHandling;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="SkipMessageErrorPolicy" />.
/// </summary>
public class SkipMessageErrorPolicyBuilder : ErrorPolicyBaseBuilder<SkipMessageErrorPolicyBuilder>
{
    /// <inheritdoc cref="ErrorPolicyBaseBuilder{TBuilder}.This" />
    protected override SkipMessageErrorPolicyBuilder This => this;

    /// <inheritdoc cref="ErrorPolicyBaseBuilder{TBuilder}.BuildCore" />
    protected override ErrorPolicyBase BuildCore() => new SkipMessageErrorPolicy();
}
