// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Diagnostics;

/// <summary>
///     The exception that is rethrown by the <see cref="FatalExceptionLoggerConsumerBehavior" /> when a fatal
///     exception occurs down the consumer pipeline.
/// </summary>
[ExcludeFromCodeCoverage]
public class ConsumerPipelineFatalException : SilverbackException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ConsumerPipelineFatalException" /> class.
    /// </summary>
    public ConsumerPipelineFatalException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConsumerPipelineFatalException" /> class with the
    ///     specified message.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    public ConsumerPipelineFatalException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="ConsumerPipelineFatalException" /> class with the
    ///     specified message and inner exception.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    /// <param name="innerException">
    ///     The inner exception.
    /// </param>
    public ConsumerPipelineFatalException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
