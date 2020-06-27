// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Diagnostics.CodeAnalysis;
using Silverback.Util;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     An header added to the message being sent over a message broker.
    /// </summary>
    public class MessageHeader
    {
        private string _name = null!; // Always being set in constructor

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageHeader" /> class.
        /// </summary>
        /// <param name="name">
        ///     The header name.
        /// </param>
        /// <param name="value">
        ///     The header value to be converted to a string.
        /// </param>
        public MessageHeader(string name, object? value)
            : this(name, value?.ToString())
        {
        }

        /// <summary>
        ///     Initializes a new instance of the <see cref="MessageHeader" /> class.
        /// </summary>
        /// <param name="name">
        ///     The header name.
        /// </param>
        /// <param name="value">
        ///     The header value.
        /// </param>
        public MessageHeader(string name, string? value)
        {
            _name = Check.NotNull(name, nameof(name));
            Value = value ?? string.Empty;
        }

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Used to deserialize")]
        private MessageHeader()
        {
        }

        /// <summary>
        ///     Gets or sets the header name.
        /// </summary>
        public string Name
        {
            get => _name;
            set => _name = Check.NotNull(value, nameof(Name));
        }

        /// <summary>
        ///     Gets or sets the header value.
        /// </summary>
        public string? Value { get; set; }
    }
}
