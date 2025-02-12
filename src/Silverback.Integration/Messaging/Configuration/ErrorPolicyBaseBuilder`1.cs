// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Messaging.Consuming.ErrorHandling;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Builds an <see cref="ErrorPolicyBase" />.
/// </summary>
/// <typeparam name="TBuilder">
///     The actual builder type.
/// </typeparam>
public abstract class ErrorPolicyBaseBuilder<TBuilder>
    where TBuilder : ErrorPolicyBaseBuilder<TBuilder>
{
    private readonly List<Type> _includedExceptions = [];

    private readonly List<Type> _excludedExceptions = [];

    private Func<IRawInboundEnvelope, Exception, bool>? _applyRule;

    private Func<IRawInboundEnvelope, Exception, object?>? _messageToPublishFactory;

    /// <summary>
    ///     Gets this instance.
    /// </summary>
    /// <remarks>
    ///     This is necessary to work around casting in the base classes.
    /// </remarks>
    protected abstract TBuilder This { get; }

    /// <summary>
    ///     Restricts the application of this policy to the specified exception type only. It is possible to
    ///     combine multiple calls to <c>ApplyTo</c> and <c>Exclude</c>.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the exception to be handled.
    /// </typeparam>
    /// <returns>
    ///     The policy builder so that additional calls can be chained.
    /// </returns>
    public TBuilder ApplyTo<T>()
        where T : Exception =>
        ApplyTo(typeof(T));

    /// <summary>
    ///     Restricts the application of this policy to the specified exception type only. It is possible to
    ///     combine multiple calls to <c>ApplyTo</c> and <c>Exclude</c>.
    /// </summary>
    /// <param name="exceptionType">
    ///     The type of the exception to be handled.
    /// </param>
    /// <returns>
    ///     The policy builder so that additional calls can be chained.
    /// </returns>
    public TBuilder ApplyTo(Type exceptionType)
    {
        _includedExceptions.Add(Check.NotNull(exceptionType, nameof(exceptionType)));
        return This;
    }

    /// <summary>
    ///     Restricts the application of this policy to all exceptions but the specified type. It is possible to
    ///     combine multiple calls to <c>ApplyTo</c> and <c>Exclude</c>.
    /// </summary>
    /// <typeparam name="T">
    ///     The type of the exception to be ignored.
    /// </typeparam>
    /// <returns>
    ///     The policy builder so that additional calls can be chained.
    /// </returns>
    public TBuilder Exclude<T>()
        where T : Exception =>
        Exclude(typeof(T));

    /// <summary>
    ///     Restricts the application of this policy to all exceptions but the specified type. It is possible to
    ///     combine multiple calls to <c>ApplyTo</c> and <c>Exclude</c>.
    /// </summary>
    /// <param name="exceptionType">
    ///     The type of the exception to be ignored.
    /// </param>
    /// <returns>
    ///     The policy builder so that additional calls can be chained.
    /// </returns>
    public TBuilder Exclude(Type exceptionType)
    {
        _excludedExceptions.Add(Check.NotNull(exceptionType, nameof(exceptionType)));
        return This;
    }

    /// <summary>
    ///     Specifies a predicate to be used to determine whether the policy has to be applied according to the
    ///     current message and exception.
    /// </summary>
    /// <param name="applyRule">
    ///     The predicate.
    /// </param>
    /// <returns>
    ///     The policy builder so that additional calls can be chained.
    /// </returns>
    public TBuilder ApplyWhen(Func<IRawInboundEnvelope, bool> applyRule)
    {
        Check.NotNull(applyRule, nameof(applyRule));
        _applyRule = (envelope, _) => applyRule.Invoke(envelope);
        return This;
    }

    /// <summary>
    ///     Specifies a predicate to be used to determine whether the policy has to be applied according to the
    ///     current message and exception.
    /// </summary>
    /// <param name="applyRule">
    ///     The predicate.
    /// </param>
    /// <returns>
    ///     The policy builder so that additional calls can be chained.
    /// </returns>
    public TBuilder ApplyWhen(Func<IRawInboundEnvelope, Exception, bool> applyRule)
    {
        Check.NotNull(applyRule, nameof(applyRule));
        _applyRule = applyRule;
        return This;
    }

    /// <summary>
    ///     Specify a factory to create a message to be published via the mediator when this policy is
    ///     applied. Useful to execute some custom code.
    /// </summary>
    /// <param name="factory">
    ///     The factory returning the message to be published.
    /// </param>
    /// <returns>
    ///     The policy builder so that additional calls can be chained.
    /// </returns>
    public TBuilder Publish(Func<IRawInboundEnvelope, object?> factory)
    {
        Check.NotNull(factory, nameof(factory));
        _messageToPublishFactory = (envelope, _) => factory.Invoke(envelope);
        return This;
    }

    /// <summary>
    ///     Specify a factory to create a message to be published via the mediator when this policy is
    ///     applied. Useful to execute some custom code.
    /// </summary>
    /// <param name="factory">
    ///     The factory returning the message to be published.
    /// </param>
    /// <returns>
    ///     The policy builder so that additional calls can be chained.
    /// </returns>
    public TBuilder Publish(Func<IRawInboundEnvelope, Exception, object?> factory)
    {
        Check.NotNull(factory, nameof(factory));
        _messageToPublishFactory = factory;
        return This;
    }

    /// <summary>
    ///     Builds the error policy instance.
    /// </summary>
    /// <returns>
    ///     The error policy.
    /// </returns>
    [SuppressMessage("Style", "IDE0305:Simplify collection initialization", Justification = "I prefer the collections to remain simple arrays")]
    public ErrorPolicyBase Build()
    {
        ErrorPolicyBase policy = BuildCore();

        return policy with
        {
            IncludedExceptions = _includedExceptions.ToArray(),
            ExcludedExceptions = _excludedExceptions.ToArray(),
            ApplyRule = _applyRule,
            MessageToPublishFactory = _messageToPublishFactory
        };
    }

    /// <summary>
    ///     Builds the error policy instance.
    /// </summary>
    /// <returns>
    ///     The error policy.
    /// </returns>
    protected abstract ErrorPolicyBase BuildCore();
}
