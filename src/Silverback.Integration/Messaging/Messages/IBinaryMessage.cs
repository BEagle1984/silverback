// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;

namespace Silverback.Messaging.Messages;

/// <summary>
///     A binary message that is being transferred over the message broker without serializing and deserializing it.
/// </summary>
public interface IBinaryMessage
{
    /// <summary>
    ///     Gets or sets the binary content.
    /// </summary>
    Stream? Content { get; set; }
}
