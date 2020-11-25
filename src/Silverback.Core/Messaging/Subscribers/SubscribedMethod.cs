// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers
{
    /// <summary>
    ///     A subscribed method that can process certain messages.
    /// </summary>
    [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
    public class SubscribedMethod
    {
        private readonly Func<IServiceProvider, object> _targetTypeFactory;

        private Type? _messageArgumentType;

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
        /// <param name="exclusive">
        ///     A boolean value indicating whether the method can be executed concurrently to other methods handling
        ///     the <b>
        ///         same message
        ///     </b>.
        /// </param>
        public SubscribedMethod(
            Func<IServiceProvider, object> targetTypeFactory,
            MethodInfo methodInfo,
            bool? exclusive)
        {
            _targetTypeFactory = Check.NotNull(targetTypeFactory, nameof(targetTypeFactory));
            MethodInfo = Check.NotNull(methodInfo, nameof(methodInfo));
            Parameters = methodInfo.GetParameters();

            if (Parameters.Count == 0)
            {
                throw new SubscribedMethodInvocationException(
                    methodInfo,
                    "The subscribed method must have at least 1 argument.");
            }

            Filters = methodInfo.GetCustomAttributes<MessageFilterAttribute>(false).ToList();

            IsExclusive = exclusive ?? true;
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
        ///     Gets a value indicating whether the method can be executed concurrently to other methods handling
        ///     the <b>same message</b>. This value is set via the <see cref="SubscribeAttribute" />.
        /// </summary>
        public bool IsExclusive { get; }

        /// <summary>
        ///     Gets the filters to be applied. The filters are set via <see cref="MessageFilterAttribute" />.
        /// </summary>
        public IReadOnlyCollection<IMessageFilter> Filters { get; }

        /// <summary>
        ///     Gets the type of the message argument (e.g. <c>MyMessage</c> or <c>IEnumerable&lt;MyMessage&gt;</c>).
        /// </summary>
        public Type MessageArgumentType =>
            _messageArgumentType ?? throw new InvalidOperationException("Not initialized.");

        /// <summary>
        ///     Gets the type of the message being subscribed.
        /// </summary>
        public Type MessageType =>
            _messageType ?? throw new InvalidOperationException("Not initialized.");

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
        ///     The target type .
        /// </returns>
        public object ResolveTargetType(IServiceProvider serviceProvider) =>
            _targetTypeFactory(serviceProvider);

        internal SubscribedMethod EnsureInitialized(IServiceProvider serviceProvider)
        {
            var argumentsResolver = serviceProvider.GetRequiredService<ArgumentsResolversRepository>();

            (_messageArgumentResolver, _messageArgumentType, _messageType) =
                argumentsResolver.GetMessageArgumentResolver(this);

            _additionalArgumentsResolvers = argumentsResolver.GetAdditionalArgumentsResolvers(this).ToArray();

            return this;
        }
    }
}
