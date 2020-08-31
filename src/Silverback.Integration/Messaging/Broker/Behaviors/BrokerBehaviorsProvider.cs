// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Behaviors
{
    /// <inheritdoc cref="IBrokerBehaviorsProvider" />
    public class BrokerBehaviorsProvider : IBrokerBehaviorsProvider
    {
        private readonly IServiceProvider _serviceProvider;

        private IReadOnlyCollection<IBrokerBehavior>? _behaviors;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BrokerBehaviorsProvider" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </param>
        public BrokerBehaviorsProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc cref="IBrokerBehaviorsProvider.CreateProducerStack" />
        public Stack<IProducerBehavior> CreateProducerStack() =>
            new Stack<IProducerBehavior>(GetBehaviors<IProducerBehavior>());

        /// <inheritdoc cref="IBrokerBehaviorsProvider.CreateConsumerStack" />
        public Stack<IConsumerBehavior> CreateConsumerStack() =>
            new Stack<IConsumerBehavior>(GetBehaviors<IConsumerBehavior>());

        private IEnumerable<TBehavior> GetBehaviors<TBehavior>()
            where TBehavior : IBrokerBehavior
        {
            // Reverse the behaviors order since they will be put in a Stack<T>
            _behaviors ??= _serviceProvider.GetServices<IBrokerBehavior>().SortBySortIndex().Reverse().ToList();

            foreach (var behavior in _behaviors)
            {
                switch (behavior)
                {
                    case TBehavior targetTypeBehavior:
                        yield return targetTypeBehavior;
                        break;
                    case IBrokerBehaviorFactory<TBehavior> behaviorFactory:
                        yield return behaviorFactory.Create();
                        break;
                }
            }
        }
    }
}
