// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration;
using Silverback.Util;

namespace Silverback.Configuration;

/// <summary>
///     Exposes the methods to configure Silverback and enable its optional features adding the needed services to the
///     <see cref="IServiceCollection" />.
/// </summary>
public partial class SilverbackBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SilverbackBuilder" /> class.
    /// </summary>
    /// <param name="services">
    ///     The <see cref="IServiceCollection" /> to be wrapped.
    /// </param>
    public SilverbackBuilder(IServiceCollection services)
    {
        Services = services;
    }

    /// <summary>
    ///     Gets the wrapped <see cref="IServiceCollection" />.
    /// </summary>
    public IServiceCollection Services { get; }

    /// <summary>
    ///     Gets the <see cref="BusOptions" />.
    /// </summary>
    public BusOptions BusOptions =>
        Services.GetSingletonServiceInstance<BusOptions>() ??
        throw new InvalidOperationException("BusOptions not found, AddSilverback has not been called.");
}
