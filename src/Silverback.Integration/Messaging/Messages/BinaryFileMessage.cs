// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Messages
{
    /// <inheritdoc cref="IBinaryFileMessage" />
    public class BinaryFileMessage : IBinaryFileMessage
    {
        /// <summary>
        ///     Initializes a new instance of the <see cref="BinaryFileMessage" /> class.
        /// </summary>
        public BinaryFileMessage()
            : this(null)
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="BinaryFileMessage" /> class with the specified content.
        /// </summary>
        /// <param name="content">
        ///     The binary content.
        /// </param>
        /// <param name="contentType">
        ///     The optional MIME type.
        /// </param>
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public BinaryFileMessage(byte[]? content, string contentType = "application/octet-stream")
        {
            Content = content;
            ContentType = contentType;
        }

        /// <summary>
        ///     Gets or sets the MIME type of the file.
        /// </summary>
        [Header(DefaultMessageHeaders.ContentType)]
        public string ContentType { get; set; }

        /// <inheritdoc />
        [SuppressMessage("", "CA1819", Justification = Justifications.CanExposeByteArray)]
        [SuppressMessage("", "SA1011", Justification = Justifications.NullableTypesSpacingFalsePositive)]
        public byte[]? Content { get; set; }
    }
}
