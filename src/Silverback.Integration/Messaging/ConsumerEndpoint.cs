// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Messaging;

/// <summary>
///     The endpoint (e.g. the topic) from which the message was consumed.
/// </summary>
public abstract record ConsumerEndpoint : Endpoint
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ConsumerEndpoint" /> class.
    /// </summary>
    /// <param name="rawName">
    ///     The endpoint name.
    /// </param>
    /// <param name="configuration">
    ///     The endpoint configuration.
    /// </param>
    protected ConsumerEndpoint(string rawName, ConsumerEndpointConfiguration configuration)
        : base(rawName, configuration)
    {
        Configuration = Check.NotNull(configuration, nameof(configuration));
    }

    /// <summary>
    ///     Gets the consumer endpoint configuration.
    /// </summary>
    public new ConsumerEndpointConfiguration Configuration { get; }
}
