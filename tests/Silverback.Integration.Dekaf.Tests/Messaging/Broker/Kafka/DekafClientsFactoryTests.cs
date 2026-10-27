// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using Shouldly;
using Silverback.Collections;
using Silverback.Messaging.Broker.Kafka;
using Silverback.Messaging.Configuration.Kafka;
using Xunit;

namespace Silverback.Tests.Integration.Dekaf.Messaging.Broker.Kafka;

public class DekafClientsFactoryTests
{
    // Dekaf only auto-commits for a joined group member: the static assignments are committed by the wrapper instead
    [Theory]
    [InlineData(true, false, true)]
    [InlineData(true, true, false)]
    [InlineData(false, false, false)]
    [InlineData(false, true, false)]
    public void UsesDekafAutoCommit_ShouldDependOnAutoCommitAndAssignment(bool enableAutoCommit, bool isStaticAssignment, bool expected)
    {
        KafkaConsumerConfiguration configuration = new()
        {
            EnableAutoCommit = enableAutoCommit,
            Endpoints = new ValueReadOnlyCollection<KafkaConsumerEndpointConfiguration>(
            [
                new KafkaConsumerEndpointConfiguration
                {
                    TopicPartitions = new ValueReadOnlyCollection<TopicPartitionOffset>(
                    [
                        new TopicPartitionOffset("topic", isStaticAssignment ? 0 : Partition.Any, Offset.Unset)
                    ])
                }
            ])
        };

        DekafClientsFactory.UsesDekafAutoCommit(configuration).ShouldBe(expected);
    }
}
