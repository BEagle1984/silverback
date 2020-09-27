// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;

namespace Silverback.Messaging.Broker.ConfluentWrappers
{
    public interface IConfluentProducerBuilder
    {
        IConfluentProducerBuilder SetConfig(ProducerConfig config);

        IConfluentProducerBuilder SetStatisticsHandler(Action<IProducer<byte[]?, byte[]?>, string> statisticsHandler);

        IProducer<byte[]?, byte[]?> Build();
    }
}
