// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     The options specifying if and when the broker clients have to be automatically connected.
/// </summary>
public class BrokerClientConnectionOptions
{
    /// <summary>
    ///     Gets or sets the <see cref="BrokerClientConnectionMode" />. The default is <see cref="BrokerClientConnectionMode.Startup" />.
    /// </summary>
    public BrokerClientConnectionMode Mode { get; set; } = BrokerClientConnectionMode.Startup;

    /// <summary>
    ///     Gets or sets a value indicating whether a retry must be performed if an exception is thrown when trying to connect. The default
    ///     is <c>true</c>. This setting is ignored when <see cref="Mode" /> is set to  <see cref="BrokerClientConnectionMode.Manual" />.
    /// </summary>
    public bool RetryOnFailure { get; set; }

    /// <summary>
    ///     Gets or sets interval between the connection retries. The default is 5 minutes. This setting is ignored when <see cref="Mode" />
    ///     is set to  <see cref="BrokerClientConnectionMode.Manual" />.
    /// </summary>
    public TimeSpan RetryInterval { get; set; } = TimeSpan.FromMinutes(5);
}
