// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Silverback.Database;

/// <summary>
///     The exception that is thrown when a required database table cannot be found via the configured data
///     layer (e.g. Entity Framework Core).
/// </summary>
[Serializable]
[ExcludeFromCodeCoverage]
public class DatabaseTableNotFoundException : SilverbackException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="DatabaseTableNotFoundException" /> class.
    /// </summary>
    public DatabaseTableNotFoundException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DatabaseTableNotFoundException" /> class with the
    ///     specified message.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    public DatabaseTableNotFoundException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DatabaseTableNotFoundException" /> class with the
    ///     specified message and inner exception.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    /// <param name="innerException">
    ///     The inner exception.
    /// </param>
    public DatabaseTableNotFoundException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="DatabaseTableNotFoundException" /> class with the
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
    protected DatabaseTableNotFoundException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
