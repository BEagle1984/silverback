// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Util;

namespace Silverback.Messaging;

/// <inheritdoc cref="ProducerEndpoint" />
public abstract record ProducerEndpoint<TConfiguration> : ProducerEndpoint
    where TConfiguration : ProducerConfiguration
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="ProducerEndpoint{TConfiguration}" /> class.
    /// </summary>
    /// <param name="rawName">
    ///     The endpoint name.
    /// </param>
    /// <param name="configuration">
    ///     The endpoint configuration.
    /// </param>
    protected ProducerEndpoint(string rawName, TConfiguration configuration)
        : base(rawName, configuration)
    {
        Configuration = Check.NotNull(configuration, nameof(configuration));
    }

    /// <inheritdoc cref="ProducerEndpoint.Configuration" />
    public new TConfiguration Configuration { get; }
}
