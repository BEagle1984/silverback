// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Configuration
{
    internal class BusConfigurator : IBusConfigurator
    {
        public BusConfigurator(BusOptions busOptions, IServiceProvider serviceProvider)
        {
            BusOptions = busOptions;
            ServiceProvider = serviceProvider;
        }

        public BusOptions BusOptions { get; }

        public IServiceProvider ServiceProvider { get; }
    }
}
