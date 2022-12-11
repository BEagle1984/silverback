// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds the <see cref="MoveMessageErrorPolicy" />.
/// </summary>
public class MoveMessageErrorPolicyBuilder : ErrorPolicyBaseBuilder<MoveMessageErrorPolicyBuilder>
{
    private readonly string _endpointName;

    private int? _maxFailedAttempts;

    private Action<IOutboundEnvelope, Exception>? _transformMessageAction;

    /// <summary>
    ///     Initializes a new instance of the <see cref="MoveMessageErrorPolicyBuilder" /> class.
    /// </summary>
    /// <param name="endpointName">
    ///     The endpoint name. It could be either the topic/queue name or the friendly name of an endpoint that is already configured with
    ///     a producer.
    /// </param>
    public MoveMessageErrorPolicyBuilder(string endpointName)
    {
        _endpointName = Check.NotNull(endpointName, nameof(endpointName));
    }

    /// <inheritdoc cref="ErrorPolicyBaseBuilder{TBuilder}.This" />
    protected override MoveMessageErrorPolicyBuilder This => this;

    /// <summary>
    ///     Sets the number of times this policy should be applied to the same message in case of multiple failed attempts.
    /// </summary>
    /// <param name="retries">
    ///     The number of times this policy should be applied.
    /// </param>
    /// <returns>
    ///     The policy builder so that additional calls can be chained.
    /// </returns>
    public MoveMessageErrorPolicyBuilder WithMaxRetries(int retries)
    {
        _maxFailedAttempts = Check.GreaterThan(retries, nameof(retries), 1);
        return this;
    }

    /// <summary>
    ///     Specify a transformation to be applied to the message before it is moved.
    /// </summary>
    /// <param name="transformationAction">
    ///     The transformation action.
    /// </param>
    /// <returns>
    ///     The policy builder so that additional calls can be chained.
    /// </returns>
    public MoveMessageErrorPolicyBuilder Transform(Action<IOutboundEnvelope?> transformationAction)
    {
        Check.NotNull(transformationAction, nameof(transformationAction));
        _transformMessageAction = (envelope, _) => transformationAction.Invoke(envelope);
        return This;
    }

    /// <summary>
    ///     Specify a transformation to be applied to the message before it is moved.
    /// </summary>
    /// <param name="transformationAction">
    ///     The transformation action.
    /// </param>
    /// <returns>
    ///     The policy builder so that additional calls can be chained.
    /// </returns>
    public MoveMessageErrorPolicyBuilder Transform(Action<IOutboundEnvelope?, Exception> transformationAction)
    {
        Check.NotNull(transformationAction, nameof(transformationAction));
        _transformMessageAction = transformationAction;
        return This;
    }

    /// <inheritdoc cref="ErrorPolicyBaseBuilder{TBuilder}.BuildCore" />
    protected override ErrorPolicyBase BuildCore() =>
        new MoveMessageErrorPolicy(_endpointName)
        {
            MaxFailedAttempts = _maxFailedAttempts,
            TransformMessageAction = _transformMessageAction
        };
}
