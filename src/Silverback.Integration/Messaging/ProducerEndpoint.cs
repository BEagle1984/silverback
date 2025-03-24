// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Messaging;

/// <summary>
///     The endpoint (e.g. the topic) where the message must be produced to.
/// </summary>
public abstract record ProducerEndpoint : Endpoint
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProducerEndpoint" /> class.
    /// </summary>
    /// <param name="rawName">
    ///     The endpoint name.
    /// </param>
    /// <param name="configuration">
    ///     The endpoint configuration.
    /// </param>
    protected ProducerEndpoint(string rawName, ProducerEndpointConfiguration configuration)
        : base(rawName, configuration)
    {
        Configuration = Check.NotNull(configuration, nameof(configuration));
    }

    /// <summary>
    ///     Gets the producer endpoint configuration.
    /// </summary>
    public new ProducerEndpointConfiguration Configuration { get; }
}
