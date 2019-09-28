// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class SilverbackBuilderExtensions
    {
        /// <summary>
        /// Registers the default messages model from Silverback.Core.Model package
        /// and the specific publishers (<see cref="ICommandPublisher"/>, <see cref="IQueryPublisher"/>,
        /// <see cref="IEventPublisher"/> and <see cref="IRequestPublisher"/>).
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ISilverbackBuilder UseModel(this ISilverbackBuilder builder)
        {
            builder.Services
                .AddScoped<ICommandPublisher, CommandPublisher>()
                .AddScoped<IQueryPublisher, QueryPublisher>()
                .AddScoped<IEventPublisher, EventPublisher>()
                .AddScoped<IRequestPublisher, RequestPublisher>();

            return builder;
        }
    }
}