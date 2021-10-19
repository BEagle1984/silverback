// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Diagnostics;
using Silverback.Messaging.Broker;

namespace Silverback.Messaging.Outbound.Routing;

internal class ProducersPreloader
{
    private readonly IBrokerCollection _brokers;

    private readonly ISilverbackLogger<ProducersPreloader> _logger;

    public ProducersPreloader(IBrokerCollection brokers, ISilverbackLogger<ProducersPreloader> logger)
    {
        _brokers = brokers;
        _logger = logger;
    }

    public void PreloadProducer(ProducerConfiguration configuration)
    {
        if (!configuration.IsValid(_logger))
            return;

        _brokers.GetProducer(configuration);
    }
}
