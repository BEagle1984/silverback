// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;

namespace Silverback.Messaging.Configuration
{
    internal class SilverbackBuilder : ISilverbackBuilder
    {
        public SilverbackBuilder(IServiceCollection services)
        {
            Services = services;
        }

        public IServiceCollection Services { get; }
    }
}
