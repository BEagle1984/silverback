// Copyright (c) 2018-2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Messaging.Subscribers.ReturnValueHandlers;

namespace Silverback.Messaging.Configuration
{
    // TODO: Test
    public class BusPluginOptions
    {
        private readonly IServiceCollection _services;

        public BusPluginOptions(IServiceCollection services)
        {
            _services = services;
        }

        public BusPluginOptions AddArgumentResolver<TInvoker>() where TInvoker : IArgumentResolver =>
            AddArgumentResolver(typeof(TInvoker));

        public BusPluginOptions AddArgumentResolver(Type invokerType)
        {
            if (invokerType == null)
                throw new ArgumentNullException(nameof(invokerType));
            if (!typeof(IArgumentResolver).IsAssignableFrom(invokerType))
                throw new ArgumentException("The invoker must implement IArgumentResolver.", nameof(invokerType));

            _services.AddScoped(typeof(IArgumentResolver), invokerType);

            return this;
        }

        public BusPluginOptions AddReturnValueHandler<TInvoker>() where TInvoker : IReturnValueHandler =>
            AddReturnValueHandler(typeof(TInvoker));

        public BusPluginOptions AddReturnValueHandler(Type handlerType)
        {
            if (handlerType == null)
                throw new ArgumentNullException(nameof(handlerType));
            if (!typeof(IReturnValueHandler).IsAssignableFrom(handlerType))
                throw new ArgumentException("The handler must implement IReturnValueHandler.", nameof(handlerType));

            _services.AddScoped(typeof(IReturnValueHandler), handlerType);

            return this;
        }
    }
}