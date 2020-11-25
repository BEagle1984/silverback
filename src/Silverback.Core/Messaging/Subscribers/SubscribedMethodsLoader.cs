﻿// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.Messaging.Configuration;
using Silverback.Messaging.Subscribers.ArgumentResolvers;
using Silverback.Util;

namespace Silverback.Messaging.Subscribers
{
    internal class SubscribedMethodsLoader
    {
        private readonly IBusOptions _options;

        private readonly IServiceProvider _serviceProvider;

        private bool? _enableMessageStreamEnumerable;

        private IReadOnlyCollection<SubscribedMethod>? _subscribedMethods;

        public SubscribedMethodsLoader(IBusOptions options, IServiceProvider serviceProvider)
        {
            _options = Check.NotNull(options, nameof(options));
            _serviceProvider = Check.NotNull(serviceProvider, nameof(serviceProvider));
        }

        public bool EnableMessageStreamEnumerable =>
            _enableMessageStreamEnumerable ??=
                GetSubscribedMethods().Any(
                    method => method.MessageArgumentResolver is IStreamEnumerableMessageArgumentResolver);

        public IReadOnlyCollection<SubscribedMethod> GetSubscribedMethods() =>
            _subscribedMethods ??=
                _options.Subscriptions
                    .SelectMany(subscription => subscription.GetSubscribedMethods(_serviceProvider))
                    .ToList();
    }
}
