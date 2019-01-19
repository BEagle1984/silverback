// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
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