// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Util;

namespace Silverback.Messaging.Configuration
{
    internal sealed class SilverbackBuilder : ISilverbackBuilder
    {
        public SilverbackBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }

        public IBusOptions BusOptions =>
            Services.GetSingletonServiceInstance<IBusOptions>() ??
            throw new InvalidOperationException("IBusOptions not found, AddSilverback has not been called.");
    }
}
