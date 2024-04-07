// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.IO;

namespace Silverback.Messaging.Messages;

/// <inheritdoc cref="IBinaryMessage" />
public class BinaryMessage : IBinaryMessage
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BinaryMessage" /> class.
    /// </summary>
    public BinaryMessage()
        : this((Stream?)null)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BinaryMessage" /> class with the specified content.
    /// </summary>
    /// <param name="content">
    ///     The binary content.
    /// </param>
    /// <param name="contentType">
    ///     The optional MIME type.
    /// </param>
    public BinaryMessage(byte[] content, string contentType = "application/octet-stream")
        : this(new MemoryStream(content), contentType)
    {
    }

    /// <summary>
    ///     Initializes a new instance of the <see cref="BinaryMessage" /> class with the specified content.
    /// </summary>
    /// <param name="content">
    ///     The binary content.
    /// </param>
    /// <param name="contentType">
    ///     The optional MIME type.
    /// </param>
    public BinaryMessage(Stream? content, string contentType = "application/octet-stream")
    {
        Content = content;
        ContentType = contentType;
    }

    /// <summary>
    ///     Gets or sets the MIME type of the file.
    /// </summary>
    [Header(DefaultMessageHeaders.ContentType)]
    public string ContentType { get; set; }

    /// <inheritdoc cref="IBinaryMessage.Content" />
    public Stream? Content { get; set; }
}
