// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Tests.Extended.Integration.Worker.Kafka;

internal static class KafkaGroupProtocolSettings
{
    // GROUP_PROTOCOL selects the classic (default) or the KIP-848 consumer protocol, while ASSIGNOR selects the client-side
    // assignor of the classic protocol. The consumer protocol rejects the session timeout, the heartbeat interval and the
    // assignment strategy, because the broker manages them.
    public static void Apply(KafkaConsumerConfigurationBuilder consumer)
    {
        string? groupProtocol = Environment.GetEnvironmentVariable("GROUP_PROTOCOL");

        if (!string.IsNullOrEmpty(groupProtocol) && Enum.Parse<GroupProtocol>(groupProtocol, true) == GroupProtocol.Consumer)
        {
            consumer.WithGroupProtocol(GroupProtocol.Consumer);
            return;
        }

        consumer
            .WithSessionTimeoutMs(6000)
            .WithHeartbeatIntervalMs(1000);

        string? assignor = Environment.GetEnvironmentVariable("ASSIGNOR");

        if (!string.IsNullOrEmpty(assignor))
            consumer.WithPartitionAssignmentStrategy(Enum.Parse<PartitionAssignmentStrategy>(assignor, true));
    }
}
