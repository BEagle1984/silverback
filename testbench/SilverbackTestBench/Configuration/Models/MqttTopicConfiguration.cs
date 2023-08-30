// Copyright (c) 2023 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.TestBench.Configuration.Models;

public record MqttTopicConfiguration(string TopicName, TimeSpan ProduceDelay = default)
    : TopicConfiguration(TopicName, ProduceDelay);
