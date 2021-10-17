// Copyright (c) 2020 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;

namespace Silverback.Messaging.Configuration.Kafka
{
    internal sealed class MockedKafkaOptions : IMockedKafkaOptions
    {
        public int DefaultPartitionsCount { get; set; } = 5;

        public int? OverriddenAutoCommitIntervalMs { get; set; } = 50;

        public TimeSpan PartitionsAssignmentDelay { get; set; } = TimeSpan.FromMilliseconds(10);
    }
}
