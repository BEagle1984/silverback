// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     The base class for the <see cref="RabbitExchangeConfig" /> and <see cref="RabbitQueueConfig" />.
    /// </summary>
    public abstract class RabbitEndpointConfig : IValidatableEndpointSettings
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
        public bool IsAutoDeleteEnabled { get; set; }

        /// <summary>
        ///     Gets or sets the optional arguments dictionary. The arguments are used by plugins and
        ///     broker-specific features to configure values such as message TTL, queue length limit, etc.
        /// </summary>
        [SuppressMessage("ReSharper", "CA2227", Justification = "DTO")]
        public Dictionary<string, object>? Arguments { get; set; }

        /// <inheritdoc cref="IValidatableEndpointSettings.Validate" />
        public virtual void Validate()
        {
        }

        /// <summary>
        ///     Determines whether the specified <see cref="RabbitEndpointConfig" /> is equal to the current
        ///     <see cref="RabbitEndpointConfig" />.
        /// </summary>
        /// <param name="other">
        ///     The object to compare with the current object.
        /// </param>
        /// <returns>
        ///     Returns a value indicating whether the other object is equal to the current object.
        /// </returns>
        protected virtual bool BaseEquals(RabbitEndpointConfig? other)
        {
            if (other is null)
                return false;

            if (ReferenceEquals(this, other))
                return true;

            return IsDurable == other.IsDurable &&
                   IsAutoDeleteEnabled == other.IsAutoDeleteEnabled &&
                   ArgumentsComparer.Equals(Arguments, other.Arguments);
        }
    }
}
