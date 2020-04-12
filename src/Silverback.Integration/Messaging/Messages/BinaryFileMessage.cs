// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

namespace Silverback.Messaging.Messages
{
    /// <inheritdoc cref="IBinaryFileMessage" />
    public class BinaryFileMessage : IBinaryFileMessage
    {
        public BinaryFileMessage()
        {
        }

        /// <summary>
        ///     Initializes a new instance of <see cref="BinaryFileMessage" /> with the specified content.
        /// </summary>
        /// <param name="content">The binary file.</param>
        /// <param name="contentType">The optional MIME type.</param>
        public BinaryFileMessage(byte[] content, string contentType = null)
        {
            Content = content;
            ContentType = contentType;
        }

        public byte[] Content { get; set; }

        /// <summary>
        ///     Gets or sets the MIME type of the file.
        /// </summary>
        [Header(DefaultMessageHeaders.ContentType)]
        public string ContentType { get; set; }
    }
}