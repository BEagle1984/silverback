// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Messaging.Subscribers.ReturnValueHandlers;

namespace Silverback.Messaging.Configuration
{
    // TODO: Test
    public class BusPluginOptions
    {
        public BusPluginOptions(IServiceCollection services)
        {
            Services = services;
        }

        internal IServiceCollection Services { get; }
    }
}