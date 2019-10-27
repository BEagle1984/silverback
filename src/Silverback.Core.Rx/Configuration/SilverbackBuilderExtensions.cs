// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Messaging.Subscribers.ReturnValueHandlers;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class SilverbackBuilderExtensions
    {
        /// <summary>
        /// Allows the subscribers to receive an <see cref="IObservable{T}"/> as parameter.
        /// It also registers the <see cref="IMessageObservable{TMessage}"/> that can be used to
        /// process the entire messages stream using Rx.NET.
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static ISilverbackBuilder AsObservable(this ISilverbackBuilder builder)
        {
            builder.Services
                .AddSingleton<IArgumentResolver, ObservableMessageArgumentResolver>()
                .AddScoped<IReturnValueHandler, ObservableMessagesReturnValueHandler>()
                .AddSingleton<MessageObservable, MessageObservable>()
                .AddSingleton(typeof(IMessageObservable<>), typeof(MessageObservable<>));
            
            builder
                .AddSingletonSubscriber<MessageObservable>();

            return builder;
        }
    }
}