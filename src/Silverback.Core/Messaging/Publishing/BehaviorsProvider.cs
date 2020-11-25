// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Util;

namespace Silverback.Messaging.Publishing
{
    /// <inheritdoc cref="IBehaviorsProvider" />
    public class BehaviorsProvider : IBehaviorsProvider
    {
        private readonly IServiceProvider _serviceProvider;

        private IReadOnlyCollection<IBehavior>? _behaviors;

        /// <summary>
        ///     Initializes a new instance of the <see cref="BehaviorsProvider" /> class.
        /// </summary>
        /// <param name="serviceProvider">
        ///     The <see cref="IServiceProvider" /> to be used to resolve the required services.
        /// </param>
        public BehaviorsProvider(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        /// <inheritdoc cref="IBehaviorsProvider.CreateStack" />
        public Stack<IBehavior> CreateStack() => new(GetBehaviors());

        private IReadOnlyCollection<IBehavior> GetBehaviors()
        {
            // Reverse the behaviors order since they will be put in a Stack<T>
            return _behaviors ??= _serviceProvider.GetServices<IBehavior>().SortBySortIndex().Reverse().ToList();
        }
    }
}
