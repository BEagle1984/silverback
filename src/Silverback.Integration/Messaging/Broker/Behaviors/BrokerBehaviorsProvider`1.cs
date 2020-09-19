// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <inheritdoc cref="IBrokerBehaviorsProvider{TBehavior}" />
    public class BrokerBehaviorsProvider<TBehavior> : IBrokerBehaviorsProvider<TBehavior>
        where TBehavior : IBrokerBehavior
    {
        private readonly IServiceProvider _serviceProvider;

        private IReadOnlyList<TBehavior>? _behaviors;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BrokerBehaviorsProvider{TBehavior}" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the behaviors.
        /// </param>
        public BrokerBehaviorsProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc cref="IBrokerBehaviorsProvider{TBehavior}.GetBehaviorsList" />
        public IReadOnlyList<TBehavior> GetBehaviorsList() =>
            _behaviors ??= _serviceProvider
                .GetServices<IBrokerBehavior>()
                .OfType<TBehavior>()
                .SortBySortIndex()
                .ToList();
    }
}
