// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Consuming.ErrorHandling;

/// <summary>
///     The exception that wraps the exception thrown by an <see cref="IErrorPolicyCallback" />.
/// </summary>
[ExcludeFromCodeCoverage]
public class ErrorPolicyCallbackInvocationException : SilverbackException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ErrorPolicyCallbackInvocationException" /> class.
    /// </summary>
    public ErrorPolicyCallbackInvocationException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ErrorPolicyCallbackInvocationException" /> class with the
    ///     specified message.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    public ErrorPolicyCallbackInvocationException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ErrorPolicyCallbackInvocationException" /> class with the
    ///     specified message and inner exception.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    /// <param name="innerException">
    ///     The inner exception.
    /// </param>
    public ErrorPolicyCallbackInvocationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
