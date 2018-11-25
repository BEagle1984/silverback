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
            .AddScoped(typeof(IEventPublisher<>), typeof(EventPublisher<>))
            .AddScoped(typeof(ICommandPublisher<>), typeof(CommandPublisher<>))
            .AddScoped(typeof(IRequestPublisher<,>), typeof(RequestPublisher<,>))
            .AddScoped(typeof(IResponsePublisher<>), typeof(ResponsePublisher<>));

        public static IServiceCollection AddEventPublisher<TEvent>(this IServiceCollection services)
            where TEvent : IEvent 
            => services.AddScoped<IEventPublisher<TEvent>, EventPublisher<TEvent>>();

        public static IServiceCollection AddCommandPublisher<TCommand>(this IServiceCollection services)
            where TCommand : ICommand 
            => services.AddScoped<ICommandPublisher<TCommand>, CommandPublisher<TCommand>>();

        public static IServiceCollection AddRequestPublisher<TRequest, TResponse>(this IServiceCollection services)
            where TRequest : IRequest
            where TResponse : IResponse 
            => services.AddScoped<IRequestPublisher<TRequest, TResponse>, RequestPublisher<TRequest, TResponse>>();

        public static IServiceCollection AddResponsePublisher<TResponse>(this IServiceCollection services)
            where TResponse : IResponse
            => services.AddScoped<IResponsePublisher<TResponse>, ResponsePublisher<TResponse>>();
    }
}