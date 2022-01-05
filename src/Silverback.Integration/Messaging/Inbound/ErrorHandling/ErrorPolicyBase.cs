// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using Silverback.Messaging.Messages;

namespace Silverback.Messaging.Inbound.ErrorHandling
{
    /// <summary>
    ///     Builds the error policy.
    /// </summary>
    public abstract class ErrorPolicyBase : IErrorPolicy
    {
        /// <summary>
        ///     Gets the number of times this policy should be applied to the same message in case of multiple failed
        ///     attempts.
        /// </summary>
        public int? MaxFailedAttemptsCount { get; internal set; }

        /// <summary>
        ///     Gets the collection of exception types this policy doesn't have to be applied to.
        /// </summary>
        public ICollection<Type> ExcludedExceptions { get; } = new List<Type>();

        /// <summary>
        ///     Gets the collection of exception types this policy have to be applied to.
        /// </summary>
        public ICollection<Type> IncludedExceptions { get; } = new List<Type>();

        /// <summary>
        ///     Gets the custom apply rule function.
        /// </summary>
        public Func<IRawInboundEnvelope, Exception, bool>? ApplyRule { get; private set; }

        /// <summary>
        ///     Gets the factory that builds the message to be published after the policy is applied.
        /// </summary>
        public Func<IRawInboundEnvelope, Exception, object?>? MessageToPublishFactory { get; private set; }

        /// <summary>
        ///     Restricts the application of this policy to the specified exception type only. It is possible to
        ///     combine multiple calls to <c>ApplyTo</c> and <c>Exclude</c>.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the exception to be handled.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="ErrorPolicyBase" /> so that additional calls can be chained.
        /// </returns>
        public ErrorPolicyBase ApplyTo<T>()
            where T : Exception
        {
            ApplyTo(typeof(T));
            return this;
        }

        /// <summary>
        ///     Restricts the application of this policy to the specified exception type only. It is possible to
        ///     combine multiple calls to <c>ApplyTo</c> and <c>Exclude</c>.
        /// </summary>
        /// <param name="exceptionType">
        ///     The type of the exception to be handled.
        /// </param>
        /// <returns>
        ///     The <see cref="ErrorPolicyBase" /> so that additional calls can be chained.
        /// </returns>
        public ErrorPolicyBase ApplyTo(Type exceptionType)
        {
            IncludedExceptions.Add(exceptionType);
            return this;
        }

        /// <summary>
        ///     Restricts the application of this policy to all exceptions but the specified type. It is possible to
        ///     combine multiple calls to <c>ApplyTo</c> and <c>Exclude</c>.
        /// </summary>
        /// <typeparam name="T">
        ///     The type of the exception to be ignored.
        /// </typeparam>
        /// <returns>
        ///     The <see cref="ErrorPolicyBase" /> so that additional calls can be chained.
        /// </returns>
        public ErrorPolicyBase Exclude<T>()
            where T : Exception
        {
            Exclude(typeof(T));
            return this;
        }

        /// <summary>
        ///     Restricts the application of this policy to all exceptions but the specified type. It is possible to
        ///     combine multiple calls to <c>ApplyTo</c> and <c>Exclude</c>.
        /// </summary>
        /// <param name="exceptionType">
        ///     The type of the exception to be ignored.
        /// </param>
        /// <returns>
        ///     The <see cref="ErrorPolicyBase" /> so that additional calls can be chained.
        /// </returns>
        public ErrorPolicyBase Exclude(Type exceptionType)
        {
            ExcludedExceptions.Add(exceptionType);
            return this;
        }

        /// <summary>
        ///     Specifies a predicate to be used to determine whether the policy has to be applied according to the
        ///     current message and exception.
        /// </summary>
        /// <param name="applyRule">
        ///     The predicate.
        /// </param>
        /// <returns>
        ///     The <see cref="ErrorPolicyBase" /> so that additional calls can be chained.
        /// </returns>
        public ErrorPolicyBase ApplyWhen(Func<IRawInboundEnvelope, Exception, bool> applyRule)
        {
            ApplyRule = applyRule;
            return this;
        }

        /// <summary>
        ///     Specify a factory to create a message to be published to the internal bus when this policy is
        ///     applied. Useful to execute some custom code.
        /// </summary>
        /// <param name="factory">
        ///     The factory returning the message to be published.
        /// </param>
        /// <returns>
        ///     The <see cref="ErrorPolicyBase" /> so that additional calls can be chained.
        /// </returns>
        public ErrorPolicyBase Publish(Func<IRawInboundEnvelope, object?> factory)
        {
            MessageToPublishFactory = (envelope, _) => factory.Invoke(envelope);
            return this;
        }

        /// <summary>
        ///     Specify a factory to create a message to be published to the internal bus when this policy is
        ///     applied. Useful to execute some custom code.
        /// </summary>
        /// <param name="factory">
        ///     The factory returning the message to be published.
        /// </param>
        /// <returns>
        ///     The <see cref="ErrorPolicyBase" /> so that additional calls can be chained.
        /// </returns>
        public ErrorPolicyBase Publish(Func<IRawInboundEnvelope, Exception, object?> factory)
        {
            MessageToPublishFactory = factory;
            return this;
        }

        /// <inheritdoc cref="IErrorPolicy.Build" />
        public IErrorPolicyImplementation Build(IServiceProvider serviceProvider) =>
            BuildCore(serviceProvider);

        /// <inheritdoc cref="IErrorPolicy.Build" />
        protected abstract ErrorPolicyImplementation BuildCore(IServiceProvider serviceProvider);
    }
}
