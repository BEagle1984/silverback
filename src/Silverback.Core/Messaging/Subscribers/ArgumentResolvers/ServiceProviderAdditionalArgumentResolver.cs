// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Subscribers.ArgumentResolvers
{
    /// <summary>
    ///     Tries to resolve the additional parameters of the subscribed methods using the
    ///     <see cref="IServiceProvider" />.
    /// </summary>
    public class ServiceProviderAdditionalArgumentResolver : IAdditionalArgumentResolver
    {
        private readonly IServiceProvider _serviceProvider;

        /// <summary>
        ///     Initializes a new instance of the <see cref="ServiceProviderAdditionalArgumentResolver" />
        ///     class.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to use to resolve the parameters value.
        /// </param>
        public ServiceProviderAdditionalArgumentResolver(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc />
        public bool CanResolve(Type parameterType) => true;

        /// <inheritdoc />
        public object GetValue(Type parameterType) => _serviceProvider.GetService(parameterType);
    }
}
