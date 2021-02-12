// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using Silverback.Util;

namespace Silverback.Messaging.Messages
{
    /// <summary>
    ///     An header added to the message being sent over a message broker.
    /// </summary>
    public sealed class MessageHeader : IEquatable<MessageHeader>
    {
        private string _name = null!; // Always being set in constructor

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

        [SuppressMessage("ReSharper", "UnusedMember.Local", Justification = "Used to be used to deserialize")]
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

        /// <inheritdoc cref="op_Equality" />
        public static bool operator ==(MessageHeader? left, MessageHeader? right) => Equals(left, right);

        /// <inheritdoc cref="op_Inequality" />
        public static bool operator !=(MessageHeader? left, MessageHeader? right) => !Equals(left, right);

        /// <inheritdoc cref="IEquatable{T}.Equals(T)" />
        public bool Equals(MessageHeader? other)
        {
            if (ReferenceEquals(null, other))
                return false;
            if (ReferenceEquals(this, other))
                return true;
            return _name == other._name && Value == other.Value;
        }

        /// <inheritdoc cref="object.Equals(object)" />
        public override bool Equals(object? obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            if (ReferenceEquals(this, obj))
                return true;
            if (obj.GetType() != GetType())
                return false;
            return Equals((MessageHeader)obj);
        }

        /// <inheritdoc cref="object.GetHashCode" />
        public override int GetHashCode() => RuntimeHelpers.GetHashCode(this);
    }
}
