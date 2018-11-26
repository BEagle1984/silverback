// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddBus(this IServiceCollection services) => services
            .AddScoped<IPublisher, Publisher>()
            .AddScoped(typeof(IEventPublisher), typeof(EventPublisher))
            .AddScoped(typeof(ICommandPublisher), typeof(CommandPublisher))
            .AddScoped(typeof(IRequestPublisher), typeof(RequestPublisher))
            .AddScoped(typeof(IQueryPublisher), typeof(QueryPublisher));
    }
}