// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Configuration;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Exposes the methods to configure the connection with the message broker(s) and add the needed services to the
///     <see cref="IServiceCollection" />.
/// </summary>
public sealed partial class BrokerOptionsBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="BrokerOptionsBuilder" /> class.
    /// </summary>
    /// <param name="silverbackBuilder">
    ///     The <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </param>
    public BrokerOptionsBuilder(SilverbackBuilder silverbackBuilder)
    {
        SilverbackBuilder = silverbackBuilder;
    }

    /// <summary>
    ///     Gets the <see cref="SilverbackBuilder" /> that references the <see cref="IServiceCollection" /> to add the services to.
    /// </summary>
    public SilverbackBuilder SilverbackBuilder { get; }
}
