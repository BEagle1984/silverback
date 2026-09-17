// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Silverback.Tests.Extended.TestBench.ViewModel.Topics;

namespace Silverback.Tests.Extended.TestBench.Producer.Messages;

public class KafkaRoutableTestBenchMessage(KafkaTopicViewModel targetTopicConfiguration) : RoutableTestBenchMessage(targetTopicConfiguration);
