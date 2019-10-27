// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Subscribers;

namespace Silverback.Messaging.Publishing
{
    public class SubscribedMethodsLoader
    {
        private readonly BusOptions _options;
        private readonly IServiceProvider _serviceProvider;

        public SubscribedMethodsLoader(BusOptions options, IServiceProvider serviceProvider)
        {
            _options = options ?? throw new ArgumentNullException(nameof(options));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public IEnumerable<SubscribedMethod> GetSubscribedMethods() =>
            _options.Subscriptions
                .SelectMany(s => s.GetSubscribedMethods(_serviceProvider))
                .ToList();
    }
}