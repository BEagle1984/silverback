// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Silverback.Messaging.Broker;
using Silverback.Messaging.Messages;
using Silverback.Util;

namespace Silverback.Messaging.Outbound.Routing
{
    internal class DefaultProduceStrategy : IProduceStrategy
    {
        private DefaultProduceStrategyImplementation? _implementation;

        public IProduceStrategyImplementation Build(IServiceProvider serviceProvider) =>
            _implementation ??= new DefaultProduceStrategyImplementation(
                serviceProvider.GetRequiredService<IBrokerCollection>(),
                serviceProvider.GetRequiredService<ILogger<DefaultProduceStrategy>>());

        private class DefaultProduceStrategyImplementation : IProduceStrategyImplementation
        {
            private readonly IBrokerCollection _brokerCollection;

            private readonly ILogger<DefaultProduceStrategy> _logger;

            public DefaultProduceStrategyImplementation(
                IBrokerCollection brokerCollection,
                ILogger<DefaultProduceStrategy> logger)
            {
                _brokerCollection = brokerCollection;
                _logger = logger;
            }

            public Task ProduceAsync(IOutboundEnvelope envelope)
            {
                Check.NotNull(envelope, nameof(envelope));

                return _brokerCollection.GetProducer(envelope.Endpoint).ProduceAsync(envelope);
            }
        }
    }
}
