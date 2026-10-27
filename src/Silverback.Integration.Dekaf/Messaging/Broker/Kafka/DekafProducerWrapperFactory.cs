// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Diagnostics;
using Silverback.Messaging.Configuration.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Broker.Kafka;

/// <summary>
///     Creates the <see cref="DekafProducerWrapper" /> instances, using the registered <see cref="IDekafClientsFactory" />.
/// </summary>
internal sealed class DekafProducerWrapperFactory : IDekafProducerWrapperFactory
{
    private readonly IDekafClientsFactory _clientsFactory;

    private readonly ISilverbackLoggerFactory _silverbackLoggerFactory;

    public DekafProducerWrapperFactory(IDekafClientsFactory clientsFactory, ISilverbackLoggerFactory silverbackLoggerFactory)
    {
        _clientsFactory = Check.NotNull(clientsFactory, nameof(clientsFactory));
        _silverbackLoggerFactory = Check.NotNull(silverbackLoggerFactory, nameof(silverbackLoggerFactory));
    }

    public IDekafProducerWrapper Create(string name, KafkaProducerConfiguration configuration) =>
        new DekafProducerWrapper(name, _clientsFactory, configuration, _silverbackLoggerFactory.CreateLogger<DekafProducerWrapper>());
}
