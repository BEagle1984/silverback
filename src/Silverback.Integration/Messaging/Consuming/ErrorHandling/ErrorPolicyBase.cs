// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Consuming.ErrorHandling;

/// <summary>
///     Builds the error policy.
/// </summary>
public abstract record ErrorPolicyBase : IErrorPolicy
{
    /// <summary>
    ///     Gets the collection of exception types this policy doesn't have to be applied to.
    /// </summary>
    public IReadOnlyCollection<Type> ExcludedExceptions { get; init; } = new List<Type>();

    /// <summary>
    ///     Gets the collection of exception types this policy have to be applied to.
    /// </summary>
    public IReadOnlyCollection<Type> IncludedExceptions { get; init; } = new List<Type>();

    /// <summary>
    ///     Gets the number of times this policy should be applied to the same message in case of multiple failed attempts.
    /// </summary>
    public int? MaxFailedAttempts { get; internal set; }

    /// <summary>
    ///     Gets the custom apply rule function.
    /// </summary>
    public Func<IRawInboundEnvelope, Exception, bool>? ApplyRule { get; init; }

    /// <summary>
    ///     Gets the factory that builds the message to be published after the policy is applied.
    /// </summary>
    public Func<IRawInboundEnvelope, Exception, object?>? MessageToPublishFactory { get; init; }

    /// <inheritdoc cref="IErrorPolicy.Build" />
    public IErrorPolicyImplementation Build(IServiceProvider serviceProvider) => BuildCore(serviceProvider);

    /// <inheritdoc cref="IErrorPolicy.Build" />
    protected abstract ErrorPolicyImplementation BuildCore(IServiceProvider serviceProvider);
}
