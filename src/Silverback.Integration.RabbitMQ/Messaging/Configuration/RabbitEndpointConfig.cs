// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Silverback.Messaging.Configuration
{
    public abstract class RabbitEndpointConfig
    {
        /// <summary>
        ///     Gets or sets a boolean value indicating whether the queue or the exchange will survive a broker restart.
        /// </summary>
        public bool IsDurable { get; set; } = true;

        /// <summary>
        ///     Gets or sets a boolean value indicating whether the queue or the exchange will be automatically deleted
        ///     when the last consumer unsubscribes.
        /// </summary>
        public bool IsAutoDeleteEnabled { get; set; } = false;

        /// <summary>
        ///     Gets or sets the optional arguments dictionary.
        ///     The arguments are used by plugins and broker-specific features to configure values such as
        ///     message TTL, queue length limit, etc.
        /// </summary>
        public Dictionary<string, object> Arguments { get; set; }

        public virtual void Validate()
        {
        }

        #region Equality

        protected bool Equals(RabbitEndpointConfig other) =>
            IsDurable == other.IsDurable &&
            IsAutoDeleteEnabled == other.IsAutoDeleteEnabled &&
            Equals(Arguments, other.Arguments);

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != GetType()) return false;
            return Equals((RabbitEndpointConfig) obj);
        }

        [SuppressMessage("ReSharper", "NonReadonlyMemberInGetHashCode")]
        public override int GetHashCode()
        {
            unchecked
            {
                var hashCode = IsDurable.GetHashCode();
                hashCode = (hashCode * 397) ^ IsAutoDeleteEnabled.GetHashCode();
                hashCode = (hashCode * 397) ^ (Arguments != null ? Arguments.GetHashCode() : 0);
                return hashCode;
            }
        }

        #endregion
    }
}