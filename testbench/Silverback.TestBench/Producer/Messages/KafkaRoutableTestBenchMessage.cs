// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.TestBench.ViewModel.Topics;

namespace Silverback.TestBench.Producer.Messages;

public class KafkaRoutableTestBenchMessage(KafkaTopicViewModel targetTopicConfiguration) : RoutableTestBenchMessage(targetTopicConfiguration);
