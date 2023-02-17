// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Serialization;

/// <summary>
///     Specifies the message encoding used by the <see cref="NewtonsoftJsonMessageSerializer" /> or
///     <see cref="NewtonsoftJsonMessageDeserializer{TMessage}"/>.
/// </summary>
public enum MessageEncoding
{
    /// <summary>
    ///     Corresponds to <see cref="System.Text.Encoding.Default" />.
    /// </summary>
    Default,

    /// <summary>
    ///     Corresponds to <see cref="System.Text.Encoding.ASCII" />.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Named after System.Text.Encoding")]
    ASCII,

    /// <summary>
    ///     Corresponds to <see cref="System.Text.Encoding.UTF8" />.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Named after System.Text.Encoding")]
    UTF8,

    /// <summary>
    ///     Corresponds to <see cref="System.Text.Encoding.UTF32" />.
    /// </summary>
    [SuppressMessage("ReSharper", "InconsistentNaming", Justification = "Named after System.Text.Encoding")]
    UTF32,

    /// <summary>
    ///     Corresponds to <see cref="System.Text.Encoding.Unicode" />.
    /// </summary>
    Unicode
}
