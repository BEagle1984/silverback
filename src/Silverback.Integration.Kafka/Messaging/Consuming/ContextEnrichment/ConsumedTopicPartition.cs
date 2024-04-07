// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Confluent.Kafka;

namespace Silverback.Messaging.Consuming.ContextEnrichment;

internal record ConsumedTopicPartition(TopicPartition TopicPartition, bool ProcessedIndependently);
