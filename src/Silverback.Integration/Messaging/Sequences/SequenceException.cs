// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Sequences;

/// <summary>
///     The exception that is thrown when a sequence cannot be properly recreated (e.g. because of bad
///     ordering or similar).
/// </summary>
[Serializable]
[ExcludeFromCodeCoverage]
public class SequenceException : SilverbackException
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SequenceException" /> class.
    /// </summary>
    public SequenceException()
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SequenceException" /> class with the specified
    ///     message.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    public SequenceException(string message)
        : base(message)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="SequenceException" /> class with the specified
    ///     message and inner exception.
    /// </summary>
    /// <param name="message">
    ///     The exception message.
    /// </param>
    /// <param name="innerException">
    ///     The inner exception.
    /// </param>
    public SequenceException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}
