// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Broker;

/// <summary>
///     The exception that is thrown when a transactional producer is used but the transaction has not been initialized.
/// </summary>
[ExcludeFromCodeCoverage]
public class MissingKafkaTransactionException : SilverbackException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="MissingKafkaTransactionException" /> class.
    /// </summary>
    public MissingKafkaTransactionException()
        : base("The transaction has not been initialized.")
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MissingKafkaTransactionException" /> class with the specified message.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    public MissingKafkaTransactionException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="MissingKafkaTransactionException" /> class with the specified message
    ///     and inner exception.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    /// <param name="innerException">
    ///     The inner exception.
    /// </param>
    public MissingKafkaTransactionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
