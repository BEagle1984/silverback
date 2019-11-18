// Copyright (c) 2019 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Messaging.Behaviors;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class SilverbackBuilderExtensions
    {
        /// <summary>
        /// Registers the fake in-memory message broker.
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="optionsAction">Additional options (such as connectors).</param>
        /// <returns></returns>
        public static ISilverbackBuilder WithInMemoryBroker(
            this ISilverbackBuilder builder,
            Action<BrokerOptionsBuilder> optionsAction = null)
        {
            builder.WithConnectionTo<InMemoryBroker>(optionsAction);

            return builder;
        }
    }
}