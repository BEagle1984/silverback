// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using Silverback.Core.Messaging;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class DependencyInjectionExtensions
    {
        public static IServiceCollection AddRxBusExtensions(this IServiceCollection services)
        {
            if (services.All(s => s.ServiceType != typeof(IPublisher)))
                services.AddBus();

            /*services
                .AddSingleton<ISubscribedMethodInvoker, Subsc>*/

            services
                .AddSingleton<MessageObservable, MessageObservable>()
                .AddSingleton<ISubscriber, MessageObservable>()
                .AddSingleton(typeof(IMessageObservable<>), typeof(MessageObservable<>));

            return services;
        }
    }
}