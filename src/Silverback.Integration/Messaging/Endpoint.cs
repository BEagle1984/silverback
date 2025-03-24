// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Messaging;

/// <summary>
///     The base class for <see cref="ProducerEndpoint" /> and <see cref="ConsumerEndpoint" />.
/// </summary>
public abstract record Endpoint
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="Endpoint" /> class.
    /// </summary>
    /// <param name="rawName">
    ///     The endpoint name.
    /// </param>
    /// <param name="configuration">
    ///     The endpoint configuration.
    /// </param>
    protected Endpoint(string rawName, EndpointConfiguration configuration)
    {
        Configuration = Check.NotNull(configuration, nameof(configuration));
        RawName = Check.NotNullOrEmpty(rawName, nameof(rawName));
        DisplayName = string.IsNullOrEmpty(Configuration.FriendlyName) ? RawName : $"{Configuration.FriendlyName} ({RawName})";
    }

    /// <summary>
    ///     Gets the endpoint name (e.g. the topic name).
    /// </summary>
    public string RawName { get; }

    /// <summary>
    ///     Gets the name to be displayed in the human-targeted output (e.g. logs, health checks result, etc.).
    /// </summary>
    public string DisplayName { get; }

    /// <summary>
    ///     Gets the endpoint configuration.
    /// </summary>
    public EndpointConfiguration Configuration { get; }
}
