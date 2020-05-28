// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Subscribers;
using Silverback.Util;

namespace Silverback.Messaging.Publishing
{
    internal class SubscribedMethodsLoader
    {
        private readonly BusOptions _options;

        private readonly IServiceProvider _serviceProvider;

        public SubscribedMethodsLoader(BusOptions options, IServiceProvider serviceProvider)
        {
            _options = Check.NotNull(options, nameof(options));
            _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        }

        public IReadOnlyCollection<SubscribedMethod> GetSubscribedMethods() =>
            _options.Subscriptions
                .SelectMany(s => s.GetSubscribedMethods(_serviceProvider))
                .ToList();
    }
}
