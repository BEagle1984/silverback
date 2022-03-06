﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Util;

namespace Silverback.Messaging.Configuration;

/// <summary>
///     Configures the producers and consumers.
/// </summary>
public partial class EndpointsConfigurationBuilder
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="EndpointsConfigurationBuilder" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" />.
    /// </param>
    public EndpointsConfigurationBuilder(IServiceProvider serviceProvider)
    {
        ServiceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
    }

    /// <summary>
    ///     Gets the <see cref="IServiceProvider" /> to be used to resolve the needed services.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }
}
