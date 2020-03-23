// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Messaging.Broker;
using Silverback.Messaging.Configuration;

// ReSharper disable once CheckNamespace
namespace Microsoft.Extensions.DependencyInjection
{
    public static class BrokerOptionsBuilderExtensions
    {
        /// <summary>
        ///     Registers the fake in-memory message broker.
        /// </summary>
        /// <param name="options"></param>
        /// <returns></returns>
        public static IBrokerOptionsBuilder AddInMemoryBroker(this IBrokerOptionsBuilder options) =>
            options.AddBroker<InMemoryBroker>();
    }
}