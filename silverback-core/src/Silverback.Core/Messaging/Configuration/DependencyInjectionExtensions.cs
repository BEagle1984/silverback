using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Messages;
using Silverback.Messaging.Publishing;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddBus(this IServiceCollection services) => services
            .AddScoped<IPublisher, Publisher>()
            .AddScoped<IEventPublisher<IEvent>, EventPublisher<IEvent>>()
            .AddScoped<ICommandPublisher<ICommand>, CommandPublisher<ICommand>>()
            .AddScoped<IResponsePublisher<IResponse>, ResponsePublisher<IResponse>>();

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