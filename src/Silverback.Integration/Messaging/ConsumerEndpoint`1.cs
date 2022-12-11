// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Messaging;

/// <inheritdoc cref="ConsumerEndpoint" />
public abstract record ConsumerEndpoint<TConfiguration> : ConsumerEndpoint
    where TConfiguration : ConsumerEndpointConfiguration
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ConsumerEndpoint{TConfiguration}" /> class.
    /// </summary>
    /// <param name="rawName">
    ///     The endpoint name.
    /// </param>
    /// <param name="configuration">
    ///     The endpoint configuration.
    /// </param>
    protected ConsumerEndpoint(string rawName, TConfiguration configuration)
        : base(rawName, configuration)
    {
        Configuration = Check.NotNull(configuration, nameof(configuration));
    }

    /// <inheritdoc cref="ConsumerEndpoint.Configuration" />
    public new TConfiguration Configuration { get; }
}
