// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Outbound
{
    internal class DefaultProduceStrategy : IProduceStrategy
    {
        // TODO: Dispose / Reset with IServiceProvider
        private DefaultProduceStrategyImplementation? _implementation;

        public IProduceStrategyImplementation Build(IServiceProvider serviceProvider) =>
            _implementation ??= new DefaultProduceStrategyImplementation(
                serviceProvider.GetRequiredService<IBrokerCollection>());

        private class DefaultProduceStrategyImplementation : IProduceStrategyImplementation
        {
            private readonly IBrokerCollection _brokerCollection;

            public DefaultProduceStrategyImplementation(IBrokerCollection brokerCollection)
            {
                _brokerCollection = brokerCollection;
            }

            public Task ProduceAsync(IOutboundEnvelope envelope)
            {
                Check.NotNull(envelope, nameof(envelope));

                return _brokerCollection.GetProducer(envelope.Endpoint).ProduceAsync(envelope);
            }
        }
    }
}
