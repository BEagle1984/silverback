// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System.Linq;
using Silverback.Core.Messaging;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Publishing;
using Silverback.Messaging.Subscribers;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Messaging.Subscribers.ReturnValueHandlers;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class BusPluginOptionsExtensions
    {
        public static BusPluginOptions Observable(this BusPluginOptions options)
        {
            options.Services
                .AddSingleton<IArgumentResolver, ObservableMessageArgumentResolver>()
                .AddSingleton<IReturnValueHandler, ObservableMessagesReturnValueHandler>()
                .AddSingleton<MessageObservable, MessageObservable>()
                .AddSingleton<ISubscriber, MessageObservable>()
                .AddSingleton(typeof(IMessageObservable<>), typeof(MessageObservable<>));

            return options;
        }
    }
}