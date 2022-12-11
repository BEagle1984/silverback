// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.Serialization;

namespace Silverback.Messaging.Subscribers;

/// <summary>
///     The exception that is thrown when a subscribed method cannot be invoked. This usually happens
///     because no value can be resolved for one or more arguments.
/// </summary>
[Serializable]
[ExcludeFromCodeCoverage]
public class SubscribedMethodInvocationException : SilverbackException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SubscribedMethodInvocationException" /> class.
    /// </summary>
    public SubscribedMethodInvocationException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SubscribedMethodInvocationException" /> class with the
    ///     specified message.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    public SubscribedMethodInvocationException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SubscribedMethodInvocationException" /> class with the
    ///     specified message and related to the specified <see cref="MethodInfo" />.
    /// </summary>
    /// <param name="methodInfo">
    ///     The <see cref="MethodInfo" /> of the related subscribed method.
    /// </param>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    public SubscribedMethodInvocationException(MethodInfo? methodInfo, string message)
        : base(
            $"Cannot invoke the subscribed method '{methodInfo?.Name}' " +
            $"in type '{methodInfo?.DeclaringType?.FullName}'. --> " +
            message)
    {
        MethodInfo = methodInfo;
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SubscribedMethodInvocationException" /> class with the
    ///     specified message and inner exception.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    /// <param name="innerException">
    ///     The inner exception.
    /// </param>
    public SubscribedMethodInvocationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SubscribedMethodInvocationException" /> class with the
    ///     serialized data.
    /// </summary>
    /// <param name="info">
    ///     The <see cref="SerializationInfo" /> that holds the serialized object data about the exception being
    ///     thrown.
    /// </param>
    /// <param name="context">
    ///     The <see cref="StreamingContext" /> that contains contextual information about the source or
    ///     destination.
    /// </param>
    protected SubscribedMethodInvocationException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }

    /// <summary>
    ///     Gets the <see cref="MethodInfo" /> representing the subscribed method whose invocation thrown the
    ///     exception.
    /// </summary>
    public MethodInfo? MethodInfo { get; }
}
