// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;

// ReSharper disable once CheckNamespace
namespace Silverback.Configuration
{
    public static class BusPluginOptionsExtensions
    {
        public static BusPluginOptions UseModel(this BusPluginOptions options)
        {
            options.Services
                .AddScoped<IEventPublisher, EventPublisher>()
                .AddScoped<ICommandPublisher, CommandPublisher>()
                .AddScoped<IRequestPublisher, RequestPublisher>()
                .AddScoped<IQueryPublisher, QueryPublisher>();

            return options;
        }
    }
}