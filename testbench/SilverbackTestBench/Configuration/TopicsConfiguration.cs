// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.TestBench.Configuration.Models;

namespace Silverback.TestBench.Configuration;

public static class TopicsConfiguration
{
    public static IReadOnlyCollection<KafkaTopicConfiguration> Kafka { get; } =
    [
        new(Topics.Kafka.Single, 8, TimeSpan.FromMilliseconds(200)),
        new(Topics.Kafka.Batch, 12, TimeSpan.FromMilliseconds(50)),
        new(Topics.Kafka.Unbounded, 8, TimeSpan.FromMilliseconds(100)),
    ];

    public static IReadOnlyCollection<MqttTopicConfiguration> Mqtt { get; } =
    [
        new(Topics.Mqtt.Topic1, TimeSpan.FromMilliseconds(200)),
        new(Topics.Mqtt.Topic2, TimeSpan.FromMilliseconds(50))
    ];

    public static IReadOnlyCollection<TopicConfiguration> All { get; } = Kafka.Cast<TopicConfiguration>().Union(Mqtt).ToArray();
}
