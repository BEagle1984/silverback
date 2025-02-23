// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Text;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Adds a method to convert a <see cref="MessageEncoding" /> enum value to an <see cref="Encoding" /> instance.
/// </summary>
public static class MessageEncodingExtensions
{
    /// <summary>
    ///     Converts the specified <see cref="MessageEncoding" /> enum value to the corresponding <see cref="Encoding" />.
    /// </summary>
    /// <param name="encoding">
    ///     The <see cref="MessageEncoding" /> value to be converted.
    /// </param>
    /// <returns>
    ///     The corresponding <see cref="Encoding" /> instance.
    /// </returns>
    public static Encoding ToEncoding(this MessageEncoding encoding) =>
        encoding switch
        {
            MessageEncoding.Default => Encoding.Default,
            MessageEncoding.ASCII => Encoding.ASCII,
            MessageEncoding.UTF8 => Encoding.UTF8,
            MessageEncoding.UTF32 => Encoding.UTF32,
            MessageEncoding.Unicode => Encoding.Unicode,
            _ => throw new ArgumentOutOfRangeException(nameof(encoding), encoding, null)
        };
}
