// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers
{
    public class ServiceProviderAdditionalArgumentResolver : IAdditionalArgumentResolver
    {
        private readonly IServiceProvider _serviceProvider;

        public ServiceProviderAdditionalArgumentResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public bool CanResolve(Type parameterType) => true;

        public object GetValue(Type parameterType) => _serviceProvider.GetService(parameterType);
    }
}