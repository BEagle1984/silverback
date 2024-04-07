// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Messaging.Subscribers.Subscriptions;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers;

/// <summary>
///     A subscribed method that can process certain messages.
/// </summary>
public class SubscribedMethod
{
    private readonly Func<IServiceProvider, object> _targetTypeFactory;

    private Type? _messageType;

    private IMessageArgumentResolver? _messageArgumentResolver;

    private IAdditionalArgumentResolver[]? _additionalArgumentsResolvers;

    /// <summary>
    ///     Initializes a new instance of the <see cref="SubscribedMethod" /> class.
    /// </summary>
    /// <param name="targetTypeFactory">
    ///     The delegate to be used to resolve an instantiate of the type declaring the subscribed method.
    /// </param>
    /// <param name="methodInfo">
    ///     The <see cref="MethodInfo" /> related to the subscribed method.
    /// </param>
    /// <param name="options">
    ///     The <see cref="SubscriptionOptions" />.
    /// </param>
    public SubscribedMethod(
        Func<IServiceProvider, object> targetTypeFactory,
        MethodInfo methodInfo,
        SubscriptionOptions options)
    {
        _targetTypeFactory = Check.NotNull(targetTypeFactory, nameof(targetTypeFactory));
        MethodInfo = Check.NotNull(methodInfo, nameof(methodInfo));
        Options = Check.NotNull(options, nameof(options));

        Parameters = methodInfo.GetParameters();
        ReturnType = MethodReturnType.CreateFromMethodInfo(methodInfo);

        if (Parameters.Count == 0)
            throw new SubscribedMethodInvocationException(methodInfo, "The subscribed method must have at least 1 argument.");
    }

    /// <summary>
    ///     Gets the <see cref="MethodInfo" /> related to the subscribed method.
    /// </summary>
    public MethodInfo MethodInfo { get; }

    /// <summary>
    ///     Gets the <see cref="ParameterInfo" /> for each parameter of the subscribed method.
    /// </summary>
    public IReadOnlyList<ParameterInfo> Parameters { get; }

    /// <summary>
    ///     Gets the <see cref="ParameterInfo" /> for the message parameter.
    /// </summary>
    public ParameterInfo MessageParameter => Parameters[0];

    /// <summary>
    ///     Gets the method return type.
    /// </summary>
    public MethodReturnType ReturnType { get; }

    /// <summary>
    ///     Gets the <see cref="SubscriptionOptions" />.
    /// </summary>
    public SubscriptionOptions Options { get; }

    /// <summary>
    ///     Gets the type of the message (or envelope) being subscribed.
    /// </summary>
    public Type MessageType => _messageType ?? throw new InvalidOperationException("Not initialized.");

    /// <summary>
    ///     Gets the <see cref="IMessageArgumentResolver" /> to be used to invoke the method.
    /// </summary>
    public IMessageArgumentResolver MessageArgumentResolver =>
        _messageArgumentResolver ?? throw new InvalidOperationException("Not initialized.");

    /// <summary>
    ///     Gets the list of <see cref="IAdditionalArgumentResolver" /> to be used to invoke the method.
    /// </summary>
    public IReadOnlyList<IAdditionalArgumentResolver> AdditionalArgumentsResolvers =>
        _additionalArgumentsResolvers ?? throw new InvalidOperationException("Not initialized.");

    /// <summary>
    ///     Resolves an instantiate of the type declaring the subscribed method.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the type or the necessary services.
    /// </param>
    /// <returns>
    ///     The target type.
    /// </returns>
    public object ResolveTargetType(IServiceProvider serviceProvider) => _targetTypeFactory.Invoke(serviceProvider);

    internal SubscribedMethod EnsureInitialized(IServiceProvider serviceProvider)
    {
        ArgumentsResolversRepository argumentsResolver = serviceProvider.GetRequiredService<ArgumentsResolversRepository>();

        (_messageArgumentResolver, _messageType) = argumentsResolver.GetMessageArgumentResolver(this);

        _additionalArgumentsResolvers = argumentsResolver.GetAdditionalArgumentsResolvers(this).ToArray();

        return this;
    }
}
