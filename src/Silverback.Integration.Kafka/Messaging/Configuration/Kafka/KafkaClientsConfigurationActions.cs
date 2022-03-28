// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Util;

namespace Silverback.Messaging.Configuration.Kafka;

internal class KafkaClientsConfigurationActions
{
    public MergeableActionCollection<KafkaProducerConfigurationBuilder> ProducerConfigurationActions { get; } = new();

    public MergeableActionCollection<KafkaConsumerConfigurationBuilder> ConsumerConfigurationActions { get; } = new();
}
