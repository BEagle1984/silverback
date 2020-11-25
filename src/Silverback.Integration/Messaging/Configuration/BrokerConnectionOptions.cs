// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Configuration
{
    /// <summary>
    ///     The options specifying if and when the message broker has to be automatically connected.
    /// </summary>
    public class BrokerConnectionOptions
    {
        /// <summary>
        ///     Gets the default options.
        /// </summary>
        public static BrokerConnectionOptions Default { get; } = new BrokerConnectionOptions();

        /// <summary>
        ///     Gets or sets the <see cref="BrokerConnectionMode" />. The default is
        ///     <see cref="BrokerConnectionMode.Startup" />.
        /// </summary>
        public BrokerConnectionMode Mode { get; set; } = BrokerConnectionMode.Startup;

        /// <summary>
        ///     Gets or sets a value indicating whether a retry must be performed if an exception is thrown when
        ///     trying to connect. The default is <c>true</c>. This setting is ignored when <c>Mode</c> is set to
        ///     manual.
        /// </summary>
        public bool RetryOnFailure { get; set; }

        /// <summary>
        ///     Gets or sets interval between the connection retries. The default is 5 minutes. This setting is
        ///     ignored
        ///     when <c>Mode</c> is set to manual.
        /// </summary>
        public TimeSpan RetryInterval { get; set; } = TimeSpan.FromMinutes(5);
    }
}
