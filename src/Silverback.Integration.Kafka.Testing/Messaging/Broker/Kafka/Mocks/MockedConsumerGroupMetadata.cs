// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Broker.Kafka.Mocks;

internal record MockedConsumerGroupMetadata(MockedConsumerGroup ConsumerGroup) : IConsumerGroupMetadata;
