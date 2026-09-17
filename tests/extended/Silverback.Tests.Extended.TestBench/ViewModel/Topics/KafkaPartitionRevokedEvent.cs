// Copyright (c) 2026 Sergio Aquilini
// This code is licensed under MIT license (see LICENSE file for details)

using System;
using Silverback.Tests.Extended.TestBench.ViewModel.Containers;

namespace Silverback.Tests.Extended.TestBench.ViewModel.Topics;

public record KafkaPartitionRevokedEvent(DateTime Timestamp, int Partition, ContainerInstanceViewModel Consumer)
    : KafkaPartitionAssignmentEvent(Timestamp, Partition, Consumer);
