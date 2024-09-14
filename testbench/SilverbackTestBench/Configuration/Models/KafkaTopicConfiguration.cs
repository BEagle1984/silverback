// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.TestBench.Configuration.Models;

public record KafkaTopicConfiguration(string TopicName, int PartitionsCount, TimeSpan ProduceDelay = default)
    : TopicConfiguration(TopicName, ProduceDelay);
