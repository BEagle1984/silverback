// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary> The base class for the <see cref="RabbitExchangeConfig"/> and <see cref="RabbitQueueConfig"/>. </summary>
    public abstract class RabbitEndpointConfig : IEquatable<RabbitEndpointConfig>, IValidatableEndpointSettings
    {
        private static readonly ConfigurationDictionaryComparer<string, object> ArgumentsComparer =
            new ConfigurationDictionaryComparer<string, object>();

        /// <summary>
        ///     Gets or sets a value indicating whether the queue or the exchange will survive a broker restart.
        /// </summary>
        public bool IsDurable { get; set; } = true;

        /// <summary>
        ///     Gets or sets a value indicating whether the queue or the exchange will be automatically deleted when
        ///     the last consumer unsubscribes.
        /// </summary>
        public bool IsAutoDeleteEnabled { get; set; } = false;

        /// <summary>
        ///     Gets or sets the optional arguments dictionary. The arguments are used by plugins and
        ///     broker-specific features to configure values such as message TTL, queue length limit, etc.
        /// </summary>
        [SuppressMessage("ReSharper", "CA2227", Justification = "DTO")]
        public Dictionary<string, object>? Arguments { get; set; }

        /// <inheritdoc />
        public virtual void Validate()
        {
        }

        /// <inheritdoc />
        public bool Equals(RabbitEndpointConfig? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return IsDurable == other.IsDurable &&
                   IsAutoDeleteEnabled == other.IsAutoDeleteEnabled &&
                   ArgumentsComparer.Equals(Arguments, other.Arguments);
        }

        /// <inheritdoc />
        public override bool Equals(object? obj)
        {
            if (obj is null)
                return false;

            if (ReferenceEquals(this, obj))
                return true;

            if (obj.GetType() != GetType())
                return false;

            return Equals((RabbitEndpointConfig)obj);
        }

        /// <inheritdoc />
        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode", Justification = Justifications.Settings)]
        public override int GetHashCode() => HashCode.Combine(IsDurable, IsAutoDeleteEnabled, Arguments);
    }
}
