// Copyright (c) 2018 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Core.Messaging;
using Silverback.Messaging.Subscribers;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddMessageObservable(this IServiceCollection services) => services
            .AddSingleton<MessageObservable, MessageObservable>()
            .AddSingleton<ISubscriber>(s => s.GetRequiredService<MessageObservable>())
            .AddSingleton(typeof(IMessageObservable<>), typeof(MessageObservable<>));
    }
}