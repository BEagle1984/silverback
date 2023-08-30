// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Collections.Generic;
using System.Linq;
using Silverback.TestBench.Configuration.Models;

namespace Silverback.TestBench.Configuration;

public static class Topics
{
    public static IReadOnlyCollection<KafkaTopicConfiguration> Kafka { get; } = new[]
    {
        new KafkaTopicConfiguration("testbench-simple", 8, TimeSpan.FromMilliseconds(200)),
        new KafkaTopicConfiguration("testbench-batch", 12, TimeSpan.FromMilliseconds(50))
    };

    public static IReadOnlyCollection<MqttTopicConfiguration> Mqtt { get; } = new[]
    {
        new MqttTopicConfiguration("testbench/topic1", TimeSpan.FromMilliseconds(200)),
        new MqttTopicConfiguration("testbench/topic2", TimeSpan.FromMilliseconds(1000))
    };

    public static IReadOnlyCollection<TopicConfiguration> All { get; } =
        Kafka.Cast<TopicConfiguration>().Union(Mqtt).ToArray();
}
