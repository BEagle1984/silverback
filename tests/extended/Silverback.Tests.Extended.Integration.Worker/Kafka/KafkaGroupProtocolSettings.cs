// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Confluent.Kafka;
using Silverback.Messaging.Configuration.Kafka;

namespace Silverback.Tests.Extended.Integration.Worker.Kafka;

internal static class KafkaGroupProtocolSettings
{
    // GROUP_PROTOCOL selects the classic (default) or the KIP-848 consumer protocol, while ASSIGNOR selects the client-side partition
    // assignment strategy of the classic protocol and SESSION_TIMEOUT_MS its session timeout (6 seconds by default). The consumer
    // protocol rejects the session timeout, the heartbeat interval and the assignment strategy, because the broker manages them.
    // GROUP_INSTANCE_ID makes the member static (KIP-345), with either protocol.
    public static void Apply(KafkaConsumerConfigurationBuilder consumer)
    {
        string? groupInstanceId = Environment.GetEnvironmentVariable("GROUP_INSTANCE_ID");

        if (!string.IsNullOrEmpty(groupInstanceId))
            consumer.WithGroupInstanceId(groupInstanceId);

        string? groupProtocol = Environment.GetEnvironmentVariable("GROUP_PROTOCOL");

        if (!string.IsNullOrEmpty(groupProtocol) && Enum.Parse<GroupProtocol>(groupProtocol, true) == GroupProtocol.Consumer)
        {
            consumer.WithGroupProtocol(GroupProtocol.Consumer);
            return;
        }

        int sessionTimeoutMs = int.TryParse(Environment.GetEnvironmentVariable("SESSION_TIMEOUT_MS"), out int value) ? value : 6000;

        consumer
            .WithSessionTimeoutMs(sessionTimeoutMs)
            .WithHeartbeatIntervalMs(1000);

        string? assignor = Environment.GetEnvironmentVariable("ASSIGNOR");

        if (!string.IsNullOrEmpty(assignor))
            consumer.WithPartitionAssignmentStrategy(Enum.Parse<PartitionAssignmentStrategy>(assignor, true));
    }
}
