// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Silverback.Storage;

/// <summary>
///     Exposes the methods to configure Silverback and enable its optional features adding the needed services to the
///     <see cref="IServiceCollection" />.
/// </summary>
public class SilverbackStorageInitializer
{
    /// <summary>
    ///     Initializes a new instance of the <see cref="SilverbackStorageInitializer" /> class.
    /// </summary>
    /// <param name="serviceProvider">
    ///     The <see cref="IServiceProvider" /> to be used to resolve the needed services.
    /// </param>
    public SilverbackStorageInitializer(IServiceProvider serviceProvider)
    {
        ServiceProvider = serviceProvider;
    }

    /// <summary>
    ///     Gets the <see cref="IServiceProvider" /> to be used to resolve the needed services.
    /// </summary>
    public IServiceProvider ServiceProvider { get; }
}
