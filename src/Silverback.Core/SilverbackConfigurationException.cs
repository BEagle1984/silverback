// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

namespace Silverback;

/// <summary>
///     The exception that is thrown when the specified configuration is not valid.
/// </summary>
[Serializable]
[ExcludeFromCodeCoverage]
public class SilverbackConfigurationException : SilverbackException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SilverbackConfigurationException" /> class.
    /// </summary>
    public SilverbackConfigurationException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SilverbackConfigurationException" /> class with the
    ///     specified message.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    public SilverbackConfigurationException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SilverbackConfigurationException" /> class with the
    ///     specified message and inner exception.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    /// <param name="innerException">
    ///     The inner exception.
    /// </param>
    public SilverbackConfigurationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SilverbackConfigurationException" /> class with the
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
    protected SilverbackConfigurationException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
