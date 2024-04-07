// Copyright (c) 2024 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using System.Diagnostics.CodeAnalysis;
using Confluent.Kafka;
using Silverback.Util;

namespace Silverback.Messaging.Consuming.ContextEnrichment;

internal static class SilverbackContextConsumedPartitionExtensions
{
    private static readonly Guid ConsumedPartitionObjectTypeId = new("225af6cd-61a2-489c-b7e1-d177c2e1a575");

    internal static void SetConsumedPartition(this SilverbackContext context, TopicPartition partition, bool processedIndependently) =>
        SetConsumedPartition(context, new ConsumedTopicPartition(partition, processedIndependently));

    internal static void SetConsumedPartition(this SilverbackContext context, ConsumedTopicPartition consumedPartition) =>
        Check.NotNull(context, nameof(context)).SetObject(ConsumedPartitionObjectTypeId, consumedPartition);

    internal static bool TryGetConsumedPartition(
        this SilverbackContext context,
        [NotNullWhen(true)] out ConsumedTopicPartition? consumedPartition) =>
        Check.NotNull(context, nameof(context)).TryGetObject(ConsumedPartitionObjectTypeId, out consumedPartition);
}
