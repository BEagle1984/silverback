// Copyright (c) 2025 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;

namespace Silverback.Diagnostics;

internal class SilverbackLoggerFactory : ISilverbackLoggerFactory
{
    private readonly IServiceProvider _serviceProvider;

    public SilverbackLoggerFactory(IServiceProvider serviceProvider)
    {
        _serviceProvider = serviceProvider;
    }

    public ISilverbackLogger<TCategoryName> CreateLogger<TCategoryName>() =>
        _serviceProvider.GetRequiredService<ISilverbackLogger<TCategoryName>>();
}
