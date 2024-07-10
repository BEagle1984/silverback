// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Producing.Routing;

/// <summary>
///     The exception that is thrown when the outbound message(s) cannot be routed.
/// </summary>
[ExcludeFromCodeCoverage]
public class RoutingException : SilverbackException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="RoutingException" /> class.
    /// </summary>
    public RoutingException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="RoutingException" /> class with the specified message.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    public RoutingException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="RoutingException" /> class with the specified message and inner exception.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    /// <param name="innerException">
    ///     The inner exception.
    /// </param>
    public RoutingException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
